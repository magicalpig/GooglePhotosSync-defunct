using VideoToolsCore.Models;
using VideoToolsCore.Services;
using VideoToolsCore.DataAccess;
using Microsoft.Extensions.Logging;

using System.Diagnostics;



namespace LocalScraper.Services;

public class Scraper(VideoContextFactory dbFactory, Settings settings, ILogger<Scraper> logger, MetadataScraper metadataScraper)
{
    private readonly VideoContext _db = dbFactory.CreateUserScopedVideoContext<LocalComputerCurrentUserService>();
    private readonly Settings _settings = settings;
    private readonly ILogger<Scraper> _logger = logger;
    private readonly MetadataScraper _metadataScraper = metadataScraper;

    public async Task ScrapeVideos()
    {
        _logger.LogInformation("Scraping Local File System for videos as user {user}", _db.GetCurrentUser() ?? "unknown");
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
        int? maxFiles = _settings.FileLimit;
        if (maxFiles > 0)
        {
            _logger.LogInformation("Limiting to {maxFiles} local files", maxFiles);
            filePaths = filePaths.Take(maxFiles.Value);
        }

        List<Task> tasks = [];
        List<VideoFileWindows> videoFiles = [];
        int countMetadataLookups = 0;
        HashSet<string> processedDirectories = [];
        Stopwatch stopwatch = new();
        stopwatch.Start();

        bool forceMetadata = false;
        if (_settings.ForceMetadataRefresh)
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
            videoFiles.Add(videoFile);
            
            bool getMetadata = forceMetadata;
            if (!getMetadata)
            {
                VideoFileWindows? existing = _db.WindowsVideos.FirstOrDefault(w => w.Path == videoFile.Path);
                bool existingIsOutdated = existing?.ModifiedBefore(videoFile) ?? true;
                // lacksMetadata won't force a metadata read b/c there are properties videos usually never have a value for.
                bool lacksMetadata = existing?.LacksMetadata() ?? true;
                _logger.LogDebug("existing: {existing}, lacks metadata: {lacksMetadata}, outdated: {outdated}", existing?.Path, lacksMetadata, existingIsOutdated);

                getMetadata = existing is null || existingIsOutdated;
            }

            if (getMetadata)
            {
                countMetadataLookups++;
                tasks.Add(_metadataScraper.GetMetadataAsync(videoFile));
            }
        }
        await Task.WhenAll(tasks);
        stopwatch.Stop();
        if (tasks.Count > 0)
        {
            _logger.LogDebug("Grabbing metadata for {countMetadataLookups} videos took {elapsed} milliseconds", tasks.Count, stopwatch.ElapsedMilliseconds);
        }


        foreach (var videoFile in videoFiles)
        {
            VideoFileWindows? existing = _db.WindowsVideos.FirstOrDefault(w => w.Path == videoFile.Path);
            if (existing is not null)
            {
                // this tells us a metadata lookup was performed. The new metadata is in videoFile right now.
                if (existing.ModifiedBefore(videoFile))
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
        _logger.LogInformation("We processed {count} local videos", videoFiles.Count);
        _db.SaveChanges();

        // Remove any videos from the database that are no longer on the local file system
        // Only do this if app was permitted to look at all local files under the specified directory.
        if (maxFiles is null || maxFiles == 0)
        {
            _logger.LogInformation("Removing videos from database that were previously found in the inspected path but are no longer there.");
            IEnumerable<VideoFileWindows> dbVideos = _db.WindowsVideos;
            foreach (string dir in processedDirectories)
            {
                var missingRecords = dbVideos.Where(v => v.Folder == dir && !filePaths.Contains(v.Path));
                foreach (var m in missingRecords)
                {
                    _logger.LogWarning("Video {m.Path} is missing from the file system", m.Path);
                    _db.Remove(m);
                }
                _logger.LogInformation("Removed {count} videos from database that were missing from the {directory} directory", missingRecords.Count(), dir);
            }
            _db.SaveChanges();
        }
    }
    
}