using FFMpegCore;
using VideoToolsCore.Models;

namespace VideoToolsCore.Services;

public interface IProvidesFFMpegPath
{
    string? FFMpegPath { get; }
}

public class FFMpegHelper
{
    // Allow up to `n` concurrent metadata fetches which invoke the ffprobe binary.
    private static readonly SemaphoreSlim s_semaphore = new(Math.Max(Environment.ProcessorCount - 1, 1));

    public FFMpegHelper(IProvidesFFMpegPath settings)
    {
        string? ffmpegPath = settings.FFMpegPath;
        if (string.IsNullOrEmpty(ffmpegPath))
        {
            throw new InvalidOperationException("No \"FFMpegPath\" setting defined for this machine. ffprobe will not be used to collect video metadata.");
        }
        
        GlobalFFOptions.Configure(options => options.BinaryFolder = ffmpegPath);
    }

    // call ffprobe to get whatever metadata we can
    internal async Task<Metadata> GetMetadataAsync(VideoFileWindows videoFile)
    {
        await s_semaphore.WaitAsync();
        try
        {
            IMediaAnalysis analysis = await FFProbe.AnalyseAsync(videoFile.Path);
            return new Metadata(analysis);            
        }
        finally
        {
            s_semaphore.Release();
        }
    }
}