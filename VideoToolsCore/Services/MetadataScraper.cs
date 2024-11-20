using FFMpegCore;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAPICodePack.Shell;
using VideoToolsCore.Models;

namespace VideoToolsCore.Services;

public class MetadataScraper(FFMpegHelper ffmpegHelper, ILogger<MetadataScraper> logger)
{
    private static readonly object s_shellMetadataLock = new();
    private readonly FFMpegHelper _ffmpegHelper = ffmpegHelper;
    private readonly ILogger<MetadataScraper> _logger = logger;

    // Combines metadata obtained through ffmpeg and shell (giving priority to the former).
    public async Task<Metadata?> GetMetadataAsync(VideoFileWindows videoFile)
    {        
        Metadata? metadataFromFFMpeg = await GetFFMpegMetadataAsync(videoFile);
        
        Metadata? metadataFromShell = null;
        if (metadataFromFFMpeg is null || metadataFromFFMpeg.IsIncomplete())
        {
            metadataFromShell = GetShellMetadata(videoFile);
        }

        // if the ffmpeg thing is not null, merge in shell metadata
        // if it is null, return the shell metadata.
        return metadataFromFFMpeg?.FillMissingFrom(metadataFromShell) ?? metadataFromShell;        
    }

    
    // Use MicrosoftAPICodePack to get metadata from Windows Shell.
    // It is not threadsafe so these calls will be made synchronously.
    // We're only doing this on videos we previous ran ffmpeg against.
    public Metadata GetShellMetadata(VideoFileWindows videoFile)
    {
        lock (s_shellMetadataLock)
        {
            string filePath = videoFile.Path;
            var shellFile = ShellFile.FromFilePath(filePath.Replace("/", @"\")); ;
            var propertiesSystem = shellFile.Properties.System;

            var newCreatedDate = propertiesSystem.DateCreated.Value ?? File.GetCreationTime(filePath);

            // Represents the actual play time of a media file and is measured in 100ns units, not milliseconds.
            var newDuration = (long)(propertiesSystem.Media.Duration?.Value / 10_000 ?? 0);

            List<string> descParts = [propertiesSystem.Title.Value, propertiesSystem.Comment.Value];
            var newDescription = string.Join(" | ", descParts.Where(s => !string.IsNullOrEmpty(s)));

            shellFile.Dispose();

            return new Metadata
            {
                DurationMs = newDuration,
                CreatedDate = newCreatedDate,
                Description = newDescription
            };
        };        
    }

    public async Task<Metadata?> GetFFMpegMetadataAsync(VideoFileWindows videoFile)
    {
        try
        {
            return await _ffmpegHelper.GetMetadataAsync(videoFile);
        }
        catch (Exception e)
        {
            _logger.LogError("Error with ffprobe on file {filePath}: {errMessage}", videoFile.Path, e.Message);
        }
        return null;
    }
}
