using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VideoToolsCore.DataAccess;
using VideoToolsCore.DataAccess.Models;
using VideoToolsCore.Metadata.Models;
using VideoToolsCore.Metadata.Services;
using VideoToolsCore.Services;
using VideoToolsCore.User.Services;

namespace MetadataGoogleToWindows.Services;

public class MetadataCopier(VideoContext videoContext, MetadataHelper metadataHelper, VideoEntityMatcher videoMatcher, ILogger<MetadataCopier> logger, IDualCurrentUserService currentUserService)
{
    private readonly VideoContext _db = videoContext;
    private readonly MetadataHelper _metadataHelper = metadataHelper;
    private readonly VideoEntityMatcher _videoMatcher = videoMatcher;
    private readonly ILogger<MetadataCopier> _logger = logger;
    private readonly IDualCurrentUserService _currentUserService = currentUserService;

    public async Task CopyMetadataAsync(string sourceDir, List<string> propsToCopy)
    {
        sourceDir = sourceDir.Replace('/', Path.DirectorySeparatorChar);
        sourceDir = sourceDir.TrimEnd(Path.DirectorySeparatorChar); // remove trailing slash if present. DB query will add its own


        var googleUser = _currentUserService.GoogleCurrentUserService?.UserId;
        var localUser = _currentUserService.LocalCurrentUserService?.UserId;
        _logger.LogInformation("Copying metadata from Google videos belonging to {gUser} to Windows videos belonging to {wUser}.\n Path is {sourceDir}\nProperties to copy are {props}", googleUser, localUser, sourceDir, propsToCopy);


        // Find all windows videos that exists in or under the source directory
        var localVideos = _db.Set<VideoFileWindows>()
            .Where(v => EF.Functions.Collate(v.Folder, "NOCASE") == sourceDir || EF.Functions.Collate(v.Folder, "NOCASE").StartsWith(sourceDir + Path.DirectorySeparatorChar))
            .ToList();

        List<Task> tasks = [];
        foreach (var localVideo in localVideos)
        {
            // ensure video file still exists
            if (!File.Exists(localVideo.Path))
            {
                _logger.LogWarning("Video file {path} no longer exists. Skipping.", localVideo.Path);
                continue;
            }

            // find the google video that best matches the windows video
            var googleVideo = await _videoMatcher.FindMatchingGoogleVideoAsync(localVideo);
            if (googleVideo == null)
            {
                _logger.LogWarning("No Google video found for Windows video {localVideo}", localVideo.Path);
                continue;
            }

            // Create a VideoMetadata with the properties to be changed set to the new values.
            var metadata = new VideoMetadata();
            foreach (var propToCopy in propsToCopy)
            {
                // validate the metadata property to copy can be mapped to a database column
                var dbColumnName = VideoMetadata.GetDbColumnForMetadataProperty(propToCopy) ?? throw new InvalidOperationException($"Metadata property \"{propToCopy}\" does not map to a database column.");

                var googleValue = googleVideo.GetType().GetProperty(dbColumnName)?.GetValue(googleVideo)?.ToString();
                var localValue = localVideo.GetType().GetProperty(dbColumnName)?.GetValue(localVideo)?.ToString();
                if (string.IsNullOrWhiteSpace(googleValue))
                {
                    _logger.LogWarning("{name}: Google video does not have a value for {prop} property", localVideo.Path, propToCopy);
                    continue;
                }
                else if (googleValue == localValue)
                {
                    _logger.LogInformation("{name}: Google video and Local video already have the same value for {prop} property", localVideo.Path, propToCopy);
                    continue;
                }
                
                _logger.LogInformation("{name}: Copying {prop} property value of \"{googleValue}\" from Google video to Local video (prop in database is currently set to \"{localValue}\")"
                    , localVideo.Path, propToCopy, googleValue, localValue);


                var metadataProperty = metadata.GetType().GetProperty(propToCopy);
                if (metadataProperty != null)
                {
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

            tasks.Add(_metadataHelper.WriteMetadataAsync(localVideo, metadata));
        }


        await Task.WhenAll(tasks);
    }
}