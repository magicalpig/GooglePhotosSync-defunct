using FFMpegCore;
using VideoToolsCore.DataAccess.Models;
using VideoToolsCore.Metadata.Models;
using VideoToolsCore.Utilities;

namespace VideoToolsCore.Metadata.Providers;


public class FFMpegMetadataProvider(IProvidesFFMpegPath settings) : FFMpegBase(settings), IMetadataProvider
{

    // call ffprobe to get whatever metadata we can
    public async Task<VideoMetadata> GetMetadataAsync(VideoFileWindows videoFile)
    {
        await s_semaphore.WaitAsync();
        try
        {
            IMediaAnalysis analysis = await FFProbe.AnalyseAsync(videoFile.Path);
            return new VideoMetadata(analysis);            
        }
        finally
        {
            s_semaphore.Release();
        }
    }

    public async Task WriteMetadataAsync(VideoFileWindows videoFile, VideoMetadata metadata)
    {
        await Task.CompletedTask;        
    }
}