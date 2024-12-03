using FFMpegCore;

namespace VideoToolsCore.Utilities;

public interface IProvidesFFMpegPath
{
    string? FFMpegPath { get; }
}

public abstract class FFMpegBase
{
    // Allow up to `n` concurrent invocations of ffprobe or ffmpeg executables
    protected static readonly SemaphoreSlim s_semaphore = new(Math.Max(Environment.ProcessorCount - 1, 1));

    protected FFMpegBase(IProvidesFFMpegPath settings)
    {    
        string? ffmpegPath = settings.FFMpegPath;
        if (string.IsNullOrEmpty(ffmpegPath))
        {
            throw new InvalidOperationException("No \"FFMpegPath\" setting defined for this machine. ffprobe will not be used to collect video metadata.");
        }
        GlobalFFOptions.Configure(options => options.BinaryFolder = ffmpegPath);
    }

}