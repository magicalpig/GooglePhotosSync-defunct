using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using VideoToolsCore.DataAccess.Models;
using VideoToolsCore.Metadata.Models;
using static Microsoft.WindowsAPICodePack.Shell.PropertySystem.ShellProperties;

namespace VideoToolsCore.Metadata.Providers;

public class ShellMetadataProvider : IMetadataProvider
{
    // Microsoft.WindowsAPICodePack.Shell is not threadsafe so no concurrent access is allowed.
    private static readonly SemaphoreSlim _shellSemaphore = new(1, 1);


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

    public async Task<VideoMetadata> GetMetadataAsync(VideoFileWindows videoFile)
    {
        await _shellSemaphore.WaitAsync();
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
            _shellSemaphore.Release();
        }
    }

    public async Task WriteMetadataAsync(VideoFileWindows videoFile, VideoMetadata metadata)
    {
        await _shellSemaphore.WaitAsync();
        string filePath = videoFile.Path;
        // keep shellfile outside the loop and defer its instantiation until we know we need it.
        ShellFile? shellFile = null;
        try
        {
            // for each value of metadata that is not null
            // confirm that the property is overwritable and maps to a shell property
            foreach (var prop in typeof(VideoMetadata).GetProperties())
            {
                object? value = prop.GetValue(metadata);
                if (value == null || !VideoMetadata.IsPropertyOverwritable(prop))
                {
                    continue;
                }

                var shellPropName = VideoMetadata.GetShellPropertyForMetadataProperty(prop);
                if (shellPropName == null)
                {
                    continue;
                }

                // finally we can instantiate the shellfile
                if (shellFile == null)
                {
                    shellFile = ShellFile.FromFilePath(filePath.Replace("/", @"\"));
                }

                using var writer = shellFile.Properties.GetPropertyWriter();
                IShellProperty? shellProp = shellFile.Properties.GetProperty(shellPropName);
                if (shellProp == null)
                {
                    continue;
                }

                if (shellProp.ValueType == typeof(string))
                {
                    writer.WriteProperty(shellProp.PropertyKey, (string)value);
                }
                else if (shellProp.ValueType == typeof(DateTime?))
                {
                    writer.WriteProperty(shellProp.PropertyKey, (DateTime)value);
                }
            }
        }
        finally
        {
            _shellSemaphore.Release();
            shellFile?.Dispose();
        }
    }


}