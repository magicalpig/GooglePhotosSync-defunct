using VideoDataAccess.Services;
using VideoDataAccess.Models;
using Microsoft.Extensions.Logging;
using FFMpegCore;
using Microsoft.WindowsAPICodePack.Shell;
using System.Diagnostics;
using ScraperCommon;


namespace LocalScraper.Services;

public class Scraper
{
  private readonly VideoContext _db;
  private readonly Settings _settings;
  private readonly ILogger<Scraper> _logger;

  public Scraper(VideoContextFactory dbFactory, Settings settings, ILogger<Scraper> logger)
  {
    _db = dbFactory.CreateUserScopedVideoContext<LocalComputerCurrentUserService>();
    _settings = settings;
    _logger = logger;
    SetFfmpegBinPath();
  }

  public async Task ScrapeVideos()
  {
    _logger.LogInformation("Scraping Local File System for videos as user {user}", _db.GetCurrentUser() ?? "<no user set>");
    if (_settings.FreshDatabase)
    {
      _logger.LogInformation("Per the FreshDatabase setting, clearing out any Windows videos from database");
      _db.RemoveAllVideos();
    }

    string? sourceDir = _settings.SourceDir; 
    if (string.IsNullOrEmpty(sourceDir)) 
    {
      string err = "appsettings.json needs to define SourceDir under the \"Settings\" key.";
      _logger.LogError("{message}", err);
      throw new ArgumentException(err);
    }
    
    if (!Directory.Exists(sourceDir))
    {
      string err = $"Directory {sourceDir} does not exist.";
      _logger.LogError("{message}", err);
      throw new ArgumentException(err);
    }
    
    // Collect video files from local file system
    _logger.LogInformation("Collecting information about video files in {sourceDir}", sourceDir);
    IEnumerable<string> filePaths = Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories)
      .Where(f => VideoFileTester.IsVideoFile(Path.GetExtension(f)))
      .Select(Path.GetFullPath)
    ;    
    // Possibly limit how many local video files are examined/saved into database
    int? maxFilesLocal = _settings.FileLimitLocal;
    if (maxFilesLocal > 0)
    {
      _logger.LogInformation("Limiting to {maxFilesLocal} local files", maxFilesLocal);
      filePaths = filePaths.Take(maxFilesLocal.Value);
    }
    
    // Allow up to `n` concurrent metadata fetches which involve invoking ffprobe bin.
    SemaphoreSlim semaphore = new(Math.Max(Environment.ProcessorCount - 1, 1));
    List<Task> tasks = [];
    List<VideoFileWindows> records = [];
    List<VideoFileWindows> ffmpegAttempts = [];
    HashSet<string> processedDirectories = [];
    Stopwatch stopwatch = new();
    stopwatch.Start();

    bool forceMetadata = false;
    if (_settings.ForceMetadataRefresh.HasValue && _settings.ForceMetadataRefresh.Value)
    {
      forceMetadata = true;
      _logger.LogWarning("Getting metadata for all local video files because ForceMetadataRefresh setting is true.");
    }

    foreach (string filePath in filePaths)
    {
      VideoFileWindows videoFile = new()
      {
        Path = filePath,
        Folder = Path.GetDirectoryName(filePath) ?? @"/",
        Name = Path.GetFileName(filePath),
        Size = new FileInfo(filePath).Length,
        LastModified = File.GetLastWriteTime(filePath)
      };
      processedDirectories.Add(videoFile.Folder);

      records.Add(videoFile);
      
      bool getMetadata = forceMetadata;
      if (!getMetadata)
      {
        VideoFileWindows? existing = _db.WindowsVideos.FirstOrDefault(w => w.Path == videoFile.Path);
        bool existingIsOutdated = existing is not null && existing.OlderThan(videoFile);
        _logger.LogDebug("existing: {existing}, lacks metadata: {lacksMetadata}, outdated: {outdated}", existing?.Path, existing?.LacksMetadata(), existingIsOutdated);
        getMetadata = existing is null || existingIsOutdated;
      } 
      
      // use ffmpeg to get metadata. We put this on the new videoFile object even if we have an existing
      // database record. Later, any existing record will be synced to match videoFile.
      if (getMetadata)
      {
        ffmpegAttempts.Add(videoFile);
        tasks.Add(
          Task.Run(async () => 
          {
            await semaphore.WaitAsync();
            try
            {
              await AttachFFProbeMetadata(videoFile);
            }
            finally
            {
              semaphore.Release();
            }
          })
        );
      }
    }
    await Task.WhenAll(tasks);
    stopwatch.Stop();
    if (tasks.Count > 0)
    {
      _logger.LogDebug("FFProbe metadata attachment for {ffmpegCallCount} videos took {elapsed} milliseconds", tasks.Count, stopwatch.ElapsedMilliseconds);  
    }
  

    // Use MicrosoftAPICodePack to get metadata ffprobe couldn't find.
    // It is not threadsafe so these calls will be made synchronously.
    // We're only doing this on videos we previous ran ffmpeg against.
    stopwatch.Reset();
    int countShell = 0;    
    foreach (var videoFile in ffmpegAttempts.Where(v => v.LacksMetadata()))
    {      
      countShell++;
      stopwatch.Start();
      AttachShellMetadata(videoFile);
      stopwatch.Stop();
    }
    if (countShell > 0)
    {
      _logger.LogDebug("Shell metadata attachment for {countShell} videos took {elapsed} milliseconds", countShell, stopwatch.ElapsedMilliseconds);
    }

    foreach (var videoFile in records)
    {
      VideoFileWindows? existing = _db.WindowsVideos.FirstOrDefault(w => w.Path == videoFile.Path);
      if (existing is not null)
      {
        // this tells us a metadata lookup was performed. The new metadata is in videoFile right now.
        if (existing.OlderThan(videoFile))
        {
          _logger.LogDebug("updating local video: {filepath}", existing.Path);
          existing.SyncTo(videoFile);
        }
        else
        {
          _logger.LogDebug("no change to local video: {filepath}", existing.Path);
        }        
      }
      else
      {
        _logger.LogInformation("new video from local file system {filepath}", videoFile.Path);
        _db.Add(videoFile);
      }
    }
    _logger.LogInformation("We processed {count} local videos", records.Count);
    _db.SaveChanges();

    // Remove any videos from the database that are no longer on the local file system
    // Only do this if app was permitted to look at all local files under the specified directory.
    if (_settings.FileLimitLocal is null || _settings.FileLimitLocal == 0)
    {
      _logger.LogInformation("Removing videos from database that are no longer on the local file system");
      IEnumerable<VideoFileWindows> dbVideos = _db.WindowsVideos;
      foreach (string dir in processedDirectories)
      {
        var missingRecords = dbVideos.Where(v => v.Folder == dir && !filePaths.Contains(v.Path));
        foreach (var m in missingRecords)
        { 
          _logger.LogWarning("Video {m.Path} is missing from the file system", m.Path);
          _db.Remove(m);
        }
      }
      _db.SaveChanges();
    }
  }


  private async Task AttachFFProbeMetadata(VideoFileWindows videoFile)
  {
    string filePath = videoFile.Path;
    // use ffprobe to get whatever metadata we can
    IMediaAnalysis? mediaInfo = null;
    try { mediaInfo = await FFProbe.AnalyseAsync(filePath); }
    catch (Exception e) 
    { 
      _logger.LogError("Error with ffprobe on file {filePath}: {errMessage}", filePath, e.Message);
      return;
    }

    // Metadata: duration
    videoFile.DurationMs = (long)mediaInfo.Duration.TotalMilliseconds;

    // Metadata: Codec
    videoFile.Codec = mediaInfo.VideoStreams.FirstOrDefault()?.CodecName ?? "unknown";

    if (mediaInfo.Format?.Tags is Dictionary<string, string> tags)
    {
      // Metadata: media creation date
      if (tags.TryGetValue("creation_time", out string? dateString)) 
      { 
        videoFile.CreatedDate = DateTime.Parse(dateString);
      }
      
      // Metadata: description. Join "title" and "comment" tags
      List<string> descParts = [ "title", "comment" ];
      videoFile.Description = string.Join(" | ", descParts
        .Select(k => tags.GetValueOrDefault(k))
        .Where(v => v is not null));      
    }
  }

  // use MicrosoftAPICodePack to get metadata
  private void AttachShellMetadata(VideoFileWindows videoFile)
  {
    string filePath = videoFile.Path;
    var shellFile = ShellFile.FromFilePath(filePath.Replace("/", @"\"));;
    var propertiesSystem = shellFile.Properties.System;
    
    videoFile.CreatedDate ??= propertiesSystem.Media.DateEncoded.Value ?? File.GetCreationTime(filePath);

    if (videoFile.DurationMs is null || videoFile.DurationMs <= 0)
    {
      // Represents the actual play time of a media file and is measured in 100ns units, not milliseconds.
      videoFile.DurationMs = (long)(propertiesSystem.Media.Duration?.Value / 10_000 ?? 0);
    }
    // Metadata: Title & Comments
    if (videoFile.Description is null)
    {
      List<string> descParts = [propertiesSystem.Title.Value, propertiesSystem.Comment.Value];
      videoFile.Description = string.Join(" | ", descParts.Where(s => !string.IsNullOrEmpty(s)));
    }
    shellFile.Dispose();
  }

  private void SetFfmpegBinPath()
  {
    string? ffmpegPath = _settings.FFMpegPath;
    if (ffmpegPath is not null)
    {
      GlobalFFOptions.Configure(options => options.BinaryFolder = ffmpegPath);
    }
    else
    {
      _logger.LogWarning("No \"FFMpegPath setting\" defined for this machine. ffprobe will not be used to collect video metadata.");
    }
  }
}