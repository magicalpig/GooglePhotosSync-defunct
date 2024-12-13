using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VideoToolsCore.DataAccess;
using VideoToolsCore.DataAccess.Models;
using VideoToolsCore.Metadata.Models;
using VideoToolsCore.Metadata.Services;
using VideoToolsCore.Services;
using VideoToolsCore.User.Services;

namespace MetadataGoogleToWindows.Services;

public class MetadataCopier(VideoContextFactory videoContextFactory, MetadataHelper metadataHelper, VideoEntityMatcher videoMatcher, ILogger<MetadataCopier> logger, IDualCurrentUserService currentUserService)
{
    private readonly VideoContextFactory _videoContextFactory = videoContextFactory;
    private readonly MetadataHelper _metadataHelper = metadataHelper;
    private readonly VideoEntityMatcher _videoMatcher = videoMatcher;
    private readonly ILogger<MetadataCopier> _logger = logger;
    private readonly IDualCurrentUserService _currentUserService = currentUserService;

    public async Task CopyMetadataAsync(string sourceDir, List<string> propsToCopy)
    {
        sourceDir = sourceDir.Replace('/', Path.DirectorySeparatorChar);
        sourceDir = sourceDir.TrimEnd(Path.DirectorySeparatorChar); // remove trailing slash if present. DB query will add its own

        var db = _videoContextFactory.CreateUserScopedVideoContext();

        var googleUser = _currentUserService.GoogleCurrentUserService?.UserId;
        var localUser = _currentUserService.LocalCurrentUserService?.UserId;
        _logger.LogInformation("Copying metadata from Google videos belonging to {gUser} to Windows videos belonging to {wUser}.\n Path is {sourceDir}\nProperties to copy are {props}",
            googleUser, localUser, sourceDir, propsToCopy);

        
        // Find all windows videos that exists in or under the source directory
        var localVideos = db.Set<VideoFileWindows>()
            .Where(v => EF.Functions.Collate(v.Folder, "NOCASE") == sourceDir || EF.Functions.Collate(v.Folder, "NOCASE").StartsWith(sourceDir + Path.DirectorySeparatorChar))
            .ToList();

        var videosToUpdate = new ConcurrentBag<VideoFileWindows>();
        await Parallel.ForEachAsync(localVideos, async (localVideo, cancellationToken) =>
        {
            // ensure video file still exists
            if (!File.Exists(localVideo.Path))
            {
                _logger.LogWarning("Video file {path} no longer exists. Skipping.", localVideo.Path);
                return;
            }

            // find the google video that best matches the windows video
            var googleVideo = await _videoMatcher.FindMatchingGoogleVideoAsync(localVideo);
            if (googleVideo == null)
            {
                _logger.LogWarning("No Google video found for Windows video {localVideo}", localVideo.Path);
                return;
            }

            // if db record for Window file is outdated, don't trust any of the saved values when comparing to Google video.
            var dbIsOutdated = localVideo.LastMetadataScanTimestamp < File.GetLastWriteTime(localVideo.Path);
            if (dbIsOutdated)
            {
                _logger.LogInformation("{name}: File has been updated since last metadata scan. Will copy all properties from Google video to Windows video.", localVideo.Path);                
            }

            // Create a VideoMetadata with the properties to be changed set to the new values.
            var metadata = new VideoMetadata();
            var anythingToCopy = false;
            foreach (var propToCopy in propsToCopy)
            {
                // validate the property to copy matches a video metadata property and is a property that can be written to the file's metadata
                if (!VideoMetadata.IsPropertyWritable(propToCopy))
                {
                    _logger.LogError("The property you're trying to copy, \"{propToCopy}\", does not map to a writable VideoMetadata property.", propToCopy);
                    continue;
                }

                var googleValue = googleVideo.GetType().GetProperty(propToCopy)?.GetValue(googleVideo)?.ToString();
                var localValue = localVideo.GetType().GetProperty(propToCopy)?.GetValue(localVideo)?.ToString();
                if (string.IsNullOrWhiteSpace(googleValue))
                {
                    _logger.LogWarning("{name}: Google video does not have a value for {prop} property", localVideo.Path, propToCopy);
                    continue;
                }
                else if (dbIsOutdated)
                {
                    
                }
                else if (googleValue == localValue)
                {
                    _logger.LogDebug("{name}: Google video and Windows video already have the same value for {prop} property: {googleValue}", localVideo.Path, propToCopy, googleValue);
                    continue;
                }
                        
                // stage the property to be copied to the Windows video by placing it in the VideoMetadata object
                var metadataProperty = metadata.GetType().GetProperty(propToCopy);
                if (metadataProperty != null)
                {
                    _logger.LogInformation("{name}: Copying {prop} property value of \"{googleValue}\" from Google video to Windows video (prop in database is currently set to \"{localValue}\")"
                    , localVideo.Path, propToCopy, googleValue, localValue);
                    anythingToCopy = true;
                    if (metadataProperty.PropertyType == typeof(DateTime?) && DateTime.TryParse(googleValue, out var googleDateTimeValue))
                    {
                        metadataProperty.SetValue(metadata, googleDateTimeValue);
                    }
                    else
                    {
                        metadataProperty.SetValue(metadata, googleValue);
                    }
                }
            }

            if (anythingToCopy)
            {
                // write metadata to file
                var anySuccessfulChanges = await _metadataHelper.WriteMetadataAsync(localVideo, metadata);
                // read full metadata from file. Attach to video and stage for database update
                if (anySuccessfulChanges)
                {
                    var metadataRefreshed = await _metadataHelper.GetMetadataAsync(localVideo);
                    if (metadataRefreshed != null)
                    {                        
                        _logger.LogDebug("Staging database update for {localVideo}.", localVideo.Path);                        
                        localVideo.AttachMetadata(metadataRefreshed);
                        videosToUpdate.Add(localVideo);                        
                    }
                }
            }
        });
    
        // db context operations must be done in single thread
        foreach (var video in videosToUpdate)
        {
            db.Attach(video);
            db.Update(video);
        }
        await db.SaveChangesAsync();        
    }
}