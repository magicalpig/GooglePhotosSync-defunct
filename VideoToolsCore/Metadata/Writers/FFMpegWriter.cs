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
        await Semaphores.FFMpegSemaphore.WaitAsync();
        Semaphores.FFMpegSemaphore.Release();
        return [];
    }
}