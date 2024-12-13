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

    public override async Task<List<string>> WriteMetadataAsync(VideoFileWindows videoFile, VideoMetadata newMetadata)
    {
        List<string> writtenFields = [];
        
        // keep extension same otherwise ffmpeg might change container type
        string? fileExtension = Path.GetExtension(videoFile.Path);
        // remove period from extension
        if (!string.IsNullOrWhiteSpace(fileExtension))
        {
            fileExtension = fileExtension[1..];
        }
        
        // Special handling for AVI files
        // FFMpeg does not retain created date even when using map metadata option.
        // So if there's no created date assigned to the newMetadata object, the only way to retain the old value
        // is to manually set it again on the new file (it lands in the "date" tag for AVI files)
        if (string.Equals(fileExtension, "avi", StringComparison.OrdinalIgnoreCase))
        {
            if (!newMetadata.CreatedDate.HasValue)
            {
                newMetadata.CreatedDate = videoFile.CreatedDate;
            }
        }        

        // TODO 3gp files don't retain Title when copied to a new file by ffmpeg.
        // The resulting file does not show a title in ffprobe but does in mediainfo and exiftool.
        // For now, with only Description and CreatedDate being copiable, this is not a problem because
        // the ShellMetadataWriter succeeds on both for a 3gp. But if I needed to use ffmpeg, the resulting
        // file would lose its title, or at least it would not have a title visible by ffprobe or Windows shell.


        string tmpOutputFileName = videoFile.Path + ".tmp." + fileExtension;

        List<string> metadataOptions = [];
        foreach (var prop in typeof(VideoMetadata).GetProperties())
        {
            // for each value of metadata that is not null
            // confirm that the property is overwritable and that this writer is capable of writing it
            object? value = prop.GetValue(newMetadata);
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
            metadataOptions.Add($"-metadata {VideoMetadata.GetMetadataWritableField(nameof(FFMpegMetadataWriter), prop, fileExtension)}=\"{value}\"");
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