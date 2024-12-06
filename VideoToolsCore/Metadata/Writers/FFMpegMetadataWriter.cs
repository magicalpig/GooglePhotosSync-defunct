using FFMpegCore;
using VideoToolsCore.DataAccess.Models;
using VideoToolsCore.Metadata.Models;
using VideoToolsCore.Utilities;

namespace VideoToolsCore.Metadata.Writers;


public class FFMpegMetadataWriter() : MetadataWriterBase
{
    public override HashSet<string> Capabilities { get; } =
    [
        "Description",
        "CreatedDate"
    ];

    public override async Task<List<string>> WriteMetadataAsync(VideoFileWindows videoFile, VideoMetadata metadata)
    {
        List<string> writtenFields = [];

        string tmpOutputFileName = videoFile.Path + ".tmp.mp4";

        List<string> metadataOptions = [];
        foreach (var prop in typeof(VideoMetadata).GetProperties())
        {
            // for each value of metadata that is not null
            // confirm that the property is overwritable that this writer is capable of writing it
            object? value = prop.GetValue(metadata);
            if (value == null || !VideoMetadata.IsPropertyWritable(prop) || !Capabilities.Contains(prop.Name))
            {
                continue;
            }
            if (value is DateTime time)
            {
                value = time.ToString("yyyy-MM-ddTHH:mm:ss");
            }
            else if (value is string str)
            {
                value = str.Replace("\"", "\\\"");
            }
            metadataOptions.Add($"-metadata {VideoMetadata.GetMetadataWritableField(nameof(FFMpegMetadataWriter), prop)}=\"{value}\"");
            writtenFields.Add(prop.Name);
        }

        await Semaphores.FFMpegSemaphore.WaitAsync();
        try
        {
            var success = await FFMpegArguments.FromFileInput(videoFile.Path)
                .MapMetaData(0)
                .OutputToFile(tmpOutputFileName, true, options => 
                {
                    options
                    .WithCustomArgument("-map 0:v") // select only the video and audio streams (some files have weird metadata streams that cause exceptions)
                    .WithCustomArgument("-map 0:a")
                    .WithCustomArgument("-c copy")
                    .OverwriteExisting();
                    foreach (var option in metadataOptions)
                    {
                        options.WithCustomArgument(option);
                    }
                })
                .ProcessAsynchronously();
            
            if (success)
            {
                File.Replace(tmpOutputFileName, videoFile.Path, null);
            }
            else
            {
                writtenFields.Clear();
            }            
        }
        finally
        {
            Semaphores.FFMpegSemaphore.Release();
        }

        return writtenFields;
    }
}