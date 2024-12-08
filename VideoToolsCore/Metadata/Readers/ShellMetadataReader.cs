using Microsoft.WindowsAPICodePack.Shell;
using VideoToolsCore.DataAccess.Models;
using VideoToolsCore.Metadata.Models;
using VideoToolsCore.Utilities;

namespace VideoToolsCore.Metadata.Readers;

public class ShellMetadataReader : IMetadataReader
{
    private static readonly Dictionary<string, string> _codecGuidMap = new()
    {
        { "{43564548-0000-0010-8000-00AA00389B71}", "hevc" },
        { "{32505657-0000-0010-8000-00AA00389B71}", "mjpeg"},
        { "{34363248-0000-0010-8000-00AA00389B71}", "h264" },
        { "{3334504D-0000-0010-8000-00AA00389B71}", "msmpeg4v3" },
        { "{3147504D-0000-0010-8000-00AA00389B71}", "mpeg1video" },
        { "{30395056-0000-0010-8000-00AA00389B71}", "vp9" },
        { "{33564D57-0000-0010-8000-00AA00389B71}", "wmv3" }
    };

    public int Priority { get; init; } = 1;

    public async Task<VideoMetadata> GetMetadataAsync(VideoFileWindows videoFile)
    {
        await Semaphores.ShellSemaphore.WaitAsync();
        try
        {
            string filePath = videoFile.Path;
            using var shellFile = ShellFile.FromFilePath(filePath.Replace("/", @"\"));
            var propertiesSystem = shellFile.Properties.System;

            DateTime createdDate = propertiesSystem.Media.DateEncoded.Value ?? File.GetCreationTime(filePath);

            // Represents the actual play time of a media file and is measured in 100ns units, not milliseconds.
            long durationMs = (long)(propertiesSystem.Media.Duration?.Value / 10_000 ?? 0);

            List<string> descParts = [propertiesSystem.Title.Value, propertiesSystem.Comment.Value];
            string description = string.Join(" | ", descParts.Where(s => !string.IsNullOrEmpty(s)));

            // The best we can get from the shell tool is the codec's GUID
            var codecGuid = propertiesSystem.Video.Compression.Value;
            string codec = string.IsNullOrEmpty(codecGuid) ? VideoMetadata.UNKNOWN_CODEC : codecGuid;
            //TODO: I get an exception when scraping mkv videos from 8mm film transfers folder
            if (_codecGuidMap.TryGetValue(codecGuid, out string? foundCodecName))
            {
                codec = foundCodecName;
            }

            return new VideoMetadata
            {
                DurationMs = durationMs,
                CreatedDate = createdDate,
                Description = description,
                Codec = codec
            };
        }
        finally
        {
            Semaphores.ShellSemaphore.Release();
        }
    }
}