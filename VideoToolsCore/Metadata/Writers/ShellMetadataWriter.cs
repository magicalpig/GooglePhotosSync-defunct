using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using VideoToolsCore.DataAccess.Models;
using VideoToolsCore.Metadata.Models;
using VideoToolsCore.Utilities;

namespace VideoToolsCore.Metadata.Writers;

public class ShellMetadataWriter : MetadataWriterBase
{
    public override HashSet<string> Capabilities { get; } =
    [
        "Description"
        // technically this writer is also capable of setting the CreatedDate, but this sets
        // the file's "Media Created" date which is not anything that ffprobe or exiftool can read.
        //, "CreatedDate"
    ];
    
    public override async Task<List<string>> WriteMetadataAsync(VideoFileWindows videoFile, VideoMetadata metadata)
    {
        await Semaphores.ShellSemaphore.WaitAsync();

        List<string> writtenProperties = [];

        string filePath = videoFile.Path;

        // keep shellfile outside the loop and defer its instantiation until we know we need it.
        ShellFile? shellFile = null;
        
        try
        {
            foreach (var prop in typeof(VideoMetadata).GetProperties())
            {
                // for each value of metadata that is not null
                // confirm that the property is overwritable that this writer is capable of writing it
                object? value = prop.GetValue(metadata);
                if (value == null || !VideoMetadata.IsPropertyWritable(prop) || !Capabilities.Contains(prop.Name))
                {
                    continue;
                }

                // translate the VideoMetadata property name to the ShellFile property name to be written
                var shellPropName = VideoMetadata.GetMetadataWritableField(nameof(ShellMetadataWriter), prop);
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

                writtenProperties.Add(prop.Name);
            }
        }
        finally
        {
            Semaphores.ShellSemaphore.Release();
            shellFile?.Dispose();
        }

        return writtenProperties;
    }
}