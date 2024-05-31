using VideoDataAccess.Services;
using VideoDataAccess.Models;
using Microsoft.Extensions.Logging;
using FFMpegCore;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using System.Diagnostics;
using Newtonsoft.Json;

namespace VidProbe.Services;

public class LocalFileSystemScraper(VideoContextFactory dbFactory, VidProbeSettings settings, ILogger<LocalFileSystemScraper> logger)
{
  private readonly VideoContext _db = dbFactory.CreateUserScopedVideoContext<LocalComputerCurrentUserService>();
  private readonly VidProbeSettings _settings = settings;
  private readonly ILogger<LocalFileSystemScraper> _logger = logger;
  private static readonly object ShellFileLock = new();
  
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
      _logger.LogError(err);
      throw new ArgumentException(err);
    }
    
    if (!Directory.Exists(sourceDir))
    {
      string err = $"Directory {sourceDir} does not exist.";
      _logger.LogError(err);
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
    
    // convert file paths to video records
    SetFfmpegBinPath();
    // Allow up to `n` concurrent metadata fetches which involve invoking ffprobe bin.
    SemaphoreSlim semaphore = new(Math.Max(Environment.ProcessorCount - 1, 1));
    List<Task> tasks = [];
    List<VideoFileWindows> records = [];
    Stopwatch stopwatch = new();
    stopwatch.Start();
    foreach (string filePath in filePaths)
    {
      tasks.Add(Task.Run(async () => 
      {
        await semaphore.WaitAsync();
        
        try
        {
          VideoFileWindows videoFile = new()
          {
              Path = filePath,
              Folder = Path.GetDirectoryName(filePath) ?? @"/",
              Name = Path.GetFileName(filePath),
              Size = new FileInfo(filePath).Length
          };
          await AttachFFProbeMetadata(filePath, videoFile);
          records.Add(videoFile);
        }
        finally
        {
          semaphore.Release();
        }
      }));
    }
    await Task.WhenAll(tasks);
    stopwatch.Stop();
    _logger.LogDebug("FFProbe metadata attachment took {elapsed} milliseconds", stopwatch.ElapsedMilliseconds);  
  
    // Use MicrosoftAPICodePack to get metadata ffprobe couldn't find.
    // It is not threadsafe so we're doing this outside the async task pool.
    stopwatch.Reset();
    stopwatch.Start();
    int countShell = 0;
    foreach (var videoFile in records)
    {
      if (!DidFFProbeFillAllMetadata(videoFile))
      {
        countShell++;
        AttachShellMetadata(videoFile.Path, videoFile);
      }
    }
    stopwatch.Stop();
    _logger.LogDebug("Shell metadata attachment for {countShell} videos took {elapsed} milliseconds", countShell, stopwatch.ElapsedMilliseconds);

    _logger.LogInformation("We processed {count} local videos", records.Count);
    
    records.ForEach(r =>
    {
      VideoFileWindows? existing = _db.GetWindowsVideos().FirstOrDefault(w => w.Path == r.Path);
      if (existing is null)
      {
        _logger.LogDebug("new video from local file system {filepath}", r.Path);
        _db.Add(r);
      }
      else if (!r.Equals(existing))
      {
        _logger.LogDebug("updating local video: {filepath}", r.Path);
        existing.SyncTo(r);
        _db.Update(existing);
      }
      else
      {
        _logger.LogDebug("no change to local video: {filepath}", r.Path);
      }
    });
    _db.SaveChanges();
  }

  private static bool DidFFProbeFillAllMetadata(VideoFileWindows videoFile)
  {
    return videoFile.CreatedDate is not null 
      && videoFile.DurationMs is not null && videoFile.DurationMs > 0
      && videoFile.Codec is not null
      && !string.IsNullOrEmpty(videoFile.Description);
  }

  private async Task AttachFFProbeMetadata(string filePath, VideoFileWindows videoFile)
  {
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
  private void AttachShellMetadata(string filePath, VideoFileWindows videoFile)
  {    
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