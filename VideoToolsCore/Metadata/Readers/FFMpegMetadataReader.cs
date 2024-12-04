using FFMpegCore;
using VideoToolsCore.DataAccess.Models;
using VideoToolsCore.Metadata.Models;
using VideoToolsCore.Utilities;

namespace VideoToolsCore.Metadata.Readers;

public class FFMpegMetadataReader() : IMetadataReader
{
    public int Priority { get; init; } = 1;

    // call ffprobe to get whatever metadata we can
    public async Task<VideoMetadata> GetMetadataAsync(VideoFileWindows videoFile)
    {
        await Semaphores.FFMpegSemaphore.WaitAsync();
        try
        {
            IMediaAnalysis analysis = await FFProbe.AnalyseAsync(videoFile.Path);
            return new VideoMetadata(analysis);            
        }
        finally
        {
            Semaphores.FFMpegSemaphore.Release();
        }
    }
}