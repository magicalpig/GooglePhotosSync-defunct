using VideoToolsCore.DataAccess.Models;
using VideoToolsCore.Services;
using VideoToolsCore.User.Services;
using VideoToolsCore.Metadata.Services;
using VideoToolsCore.DataAccess;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;



namespace LocalScraper.Services;

public class Scraper(VideoContextFactory dbFactory, Settings settings, ILogger<Scraper> logger, MetadataHelper metadataScraper, IDualCurrentUserService currentUserService)
{
    private readonly VideoContext _db = dbFactory.CreateUserScopedVideoContext();
    private readonly Settings _settings = settings;
    private readonly ILogger<Scraper> _logger = logger;
    private readonly MetadataHelper _metadataScraper = metadataScraper;
    private readonly IDualCurrentUserService _currentUserService = currentUserService;

    public async Task ScrapeVideos()
    {
        if (_settings.FreshDatabase)
        {
            _logger.LogInformation("Per the FreshDatabase setting, clearing out any Windows videos from database");
            _db.RemoveAllVideos();
        }

        string sourceDir = _settings.SourceDir ?? throw new ArgumentException("\"SourceDir\" setting not found in your configuration.");
        
        if (!Directory.Exists(sourceDir))
        {
            throw new ArgumentException($"Directory {sourceDir} does not exist.");
        }

        // Collect video files from local file system
        _logger.LogInformation("Collecting information about video files in {sourceDir} as user {u}", sourceDir, _currentUserService.GetCurrentUserForEntity(new VideoFileWindows()));
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
        List<string> scannedDirectories = Directory.EnumerateDirectories(sourceDir, "*", SearchOption.AllDirectories).ToList();
        scannedDirectories.Add(sourceDir);
        // normalize slashes the same way we're doing for each file's path.
        scannedDirectories = scannedDirectories.Select(d => Path.GetFullPath(d)).ToList();
        
        bool forceMetadata = false;
        if (_settings.ForceMetadataRefresh)
        {
            forceMetadata = true;
            _logger.LogWarning("Getting metadata for all local video files because ForceMetadataRefresh setting is true.");
        }

        Stopwatch stopwatch = new();
        stopwatch.Start();
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

            videoFiles.Add(videoFile);

            bool getMetadata = forceMetadata;
            if (!getMetadata)
            {
                VideoFileWindows? existing = _db.WindowsVideos.FirstOrDefault(w => EF.Functions.Collate(w.Path, "NOCASE") == videoFile.Path);
                bool existingIsOutdated = existing?.ModifiedBefore(videoFile) ?? true;
                bool lacksMetadata = existing?.LacksStandardMetadata() ?? true;
                _logger.LogDebug("File: {file}: exists: {existing}, lacks standard metadata: {lacksMetadata}, outdated: {outdated}", videoFile.Path, existing?.Path, lacksMetadata, existingIsOutdated);

                getMetadata = existing is null || lacksMetadata || existingIsOutdated;
            }

            if (getMetadata)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var metadata = await _metadataScraper.GetMetadataAsync(videoFile);
                    if (metadata is not null)
                    {
                        videoFile.AttachMetadata(metadata);
                    }
                }));
            }
        }
        await Task.WhenAll(tasks);
        stopwatch.Stop();
        if (tasks.Count > 0)
        {
            _logger.LogDebug("Grabbing metadata for {c} videos took {elapsed} milliseconds", tasks.Count, stopwatch.ElapsedMilliseconds);
        }


        foreach (var videoFile in videoFiles)
        {
            VideoFileWindows? existing = _db.WindowsVideos.FirstOrDefault(w => EF.Functions.Collate(w.Path, "NOCASE") == videoFile.Path);
            if (existing is not null)
            {
                // this tells us a metadata lookup was performed. The new metadata is in videoFile right now.
                if (existing.LastMetadataScanTimestamp == null || existing.LastMetadataScanTimestamp < videoFile.LastMetadataScanTimestamp)
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

        // Remove db records for videos no longer present under the scanned directory.
        // Only do this if there was no limit on number of files to process.
        if (maxFiles is null || maxFiles == 0)
        {
            _logger.LogDebug("Removing videos from database that were previously found under the inspected path but are no longer there.");
            var dbVideos = _db.WindowsVideos.ToList();
            var missingRecords = dbVideos.Where(v => scannedDirectories.Contains(v.Folder) && !filePaths.Contains(v.Path)).ToList();

            if (missingRecords.Count > 0)
            {
                _logger.LogWarning("Videos missing from the scanned directories ({count}): {missingRecords}", missingRecords.Count, missingRecords.Select(v => v.Path));
                _db.WindowsVideos.RemoveRange(missingRecords);
            }
        }

        // Check outside the scanned directory for any deleted directories being referenced in the database
        _logger.LogDebug("Looking for records in deleted directories");        
        var allDbDirectories = _db.WindowsVideos.Select(v => v.Folder).Distinct().ToList();
        var missingDirectories = allDbDirectories.Except(scannedDirectories).Where(dir => !Directory.Exists(dir)).ToList();
        foreach (var dir in missingDirectories)
        {
            var missingRecords = _db.WindowsVideos.Where(v => v.Folder == dir);
            foreach (var m in missingRecords)
            {
                _logger.LogWarning("Video {m.Path} is missing from the file system", m.Path);
                _db.Remove(m);
            }
            _logger.LogWarning("Removed {count} videos from database that were missing from the {directory} directory", missingRecords.Count(), dir);
        }

        _db.SaveChanges();
    }
}