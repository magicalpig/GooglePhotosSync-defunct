using Microsoft.Extensions.Logging;
using VideoToolsCore.DataAccess.Models;
using VideoToolsCore.Metadata.Providers;
using VideoToolsCore.Metadata.Models;

namespace VideoToolsCore.Metadata.Services;

public class MetadataHelper(ILogger<MetadataHelper> logger, IEnumerable<IMetadataProvider> metadataProviders)
{
    private readonly IMetadataProvider? _ffmpegHelper = metadataProviders.FirstOrDefault(h => h.GetType() == typeof(FFMpegMetadataProvider));
    private readonly IMetadataProvider _shellHelper = metadataProviders.First(h => h.GetType() == typeof(ShellMetadataProvider));
    private readonly ILogger<MetadataHelper> _logger = logger;

    // Combines metadata obtained through ffmpeg and shell (giving priority to the former).
    public async Task<VideoMetadata?> GetMetadataAsync(VideoFileWindows videoFile)
    {
        VideoMetadata? metadataFromFFMpeg = null;
        if (_ffmpegHelper is not null)
        {
            try
            {
                metadataFromFFMpeg = await _ffmpegHelper.GetMetadataAsync(videoFile);
            }
            catch (Exception e)
            {
                _logger.LogError("Error with ffprobe on file {filePath}: {errMessage}", videoFile.Path, e.Message);
            }
        }

        // Use MicrosoftAPICodePack to get metadata from Windows Shell.
        // We're only doing this on videos we previously ran ffmpeg against.
        VideoMetadata? metadataFromShell = null;
        if (metadataFromFFMpeg is null || metadataFromFFMpeg.IsIncomplete())
        {
            metadataFromShell = await _shellHelper.GetMetadataAsync(videoFile);
        }

        // if the ffmpeg thing is not null, merge in shell metadata
        // if it is null, return the shell metadata.
        return metadataFromFFMpeg?.FillMissingFrom(metadataFromShell) ?? metadataFromShell;
    }

    // updates video file metadata using shell tool rather than ffmpeg. The latter cannot change file in-place.
    public async Task WriteMetadataAsync(VideoFileWindows videoFile, VideoMetadata metadata)
    {
        // TODO: there's a problem here with updating the media create date using the shell library:
        // This will update the "media created" shell property which is not reflected in any exif data or any 
        // other metadata that ffmpeg can read. So, if the file already has an encoded date that ffmpeg can read,
        // that will take precedence when scraping the metadata for the file 
        // assuming FfmpegMetadataProvider is a registered service for the app.
        await _shellHelper.WriteMetadataAsync(videoFile, metadata);
    }

    // writes to a database entity, not to the filesystem nor Google Photos.
    public async Task GetAndAttachMetadataAsync(VideoFileWindows videoFile)
    {
        VideoMetadata? metadata = await GetMetadataAsync(videoFile);
        if (metadata is not null)
        {
            videoFile.AttachMetadata(metadata);
        }
    }
}
