using FFMpegCore;
using VideoToolsCore.Models;

namespace VideoToolsCore.Services;

public interface IProvidesFFMpegPath
{
    string? FFMpegPath { get; }
}

public class FFMpegHelper
{
    private static bool s_isInitialized = false;
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
        s_isInitialized = true;
    }

    // call ffprobe to get whatever metadata we can
    public async Task<IMediaAnalysis> GetMetadataAsync(VideoFileWindows videoFile)
    {
        if (!s_isInitialized)
        {
            throw new InvalidOperationException("FFMpegHelper not initialized. Call Initialize() first.");
        }
        await s_semaphore.WaitAsync();
        try
        {
            string filePath = videoFile.Path;
            return await FFProbe.AnalyseAsync(filePath);
        }
        finally
        {
            s_semaphore.Release();
        }
    }
}