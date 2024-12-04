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

    public async Task CopyMetadataAsync(string sourceDir, string propToCopy)
    {
        sourceDir = sourceDir.Replace('/', Path.DirectorySeparatorChar);
        sourceDir = sourceDir.TrimEnd(Path.DirectorySeparatorChar); // remove trailing slash if present. DB query will add its own

        // validate the metadata property to copy can be mapped to a database column
        var dbColumnName = VideoMetadata.GetDbColumnForMetadataProperty(propToCopy) ?? throw new InvalidOperationException($"Metadata property \"{propToCopy}\" does not map to a database column.");

        var googleUser = _currentUserService.GoogleCurrentUserService?.UserId;
        var localUser = _currentUserService.LocalCurrentUserService?.UserId;
        _logger.LogInformation("Copying metadata from Google videos belonging to {gUser} to Windows videos belonging to {wUser}. Path is {sourceDir} and property is {prop}", googleUser, localUser, sourceDir, propToCopy);

        // Find all google videos that have the property to copy defined
        var googleVideosQuery = _db.Set<VideoFileGoogle>()
          .Where(v => EF.Property<string>(v, propToCopy) != null && EF.Property<string>(v, propToCopy) != "");

        // Find all windows videos that exists in or under the source directory
        var localVideos = _db.Set<VideoFileWindows>()
            .Where(v => EF.Functions.Collate(v.Folder, "NOCASE") == sourceDir || EF.Functions.Collate(v.Folder, "NOCASE").StartsWith(sourceDir + Path.DirectorySeparatorChar))
            .ToList();


        foreach (var localVideo in localVideos)
        {
            // ensure video file still exists
            if (!File.Exists(localVideo.Path))
            {
                _logger.LogWarning("Video file {path} no longer exists. Skipping.", localVideo.Path);
                continue;
            }

            // find the google video that best matches the windows video
            var googleVideo = await _videoMatcher.FindMatchingGoogleVideoAsync(localVideo, googleVideosQuery);
            if (googleVideo == null)
            {
                _logger.LogWarning("No Google video found for Windows video {localVideo}", localVideo.Path);
                continue;
            }

            // load existing source and destination values from the database
            var googleValue = googleVideo.GetType().GetProperty(dbColumnName)?.GetValue(googleVideo)?.ToString();
            var localValue = localVideo.GetType().GetProperty(dbColumnName)?.GetValue(localVideo)?.ToString();
            _logger.LogInformation("{name}: Copying {prop} property value of \"{googleValue}\" from Google video to Local video (prop in database is currently set to \"{localValue}\")"
                , localVideo.Path, propToCopy, googleValue, localValue);


            // Create a VideoMetadata object that has the property to be changed defined with the new value.
            var metadata = new VideoMetadata();
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
            
            await _metadataHelper.WriteMetadataAsync(localVideo, metadata);
        }


        await Task.CompletedTask;
    }
}