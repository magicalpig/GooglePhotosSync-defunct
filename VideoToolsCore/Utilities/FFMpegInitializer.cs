using FFMpegCore;

namespace VideoToolsCore.Utilities;

public interface IProvidesFFMpegPath
{
    string? FFMpegPath { get; }
}

internal static class FFMpegInitializer
{
    internal static void Initialize(IProvidesFFMpegPath settings)
    {    
        string? ffmpegPath = settings.FFMpegPath;
        if (string.IsNullOrEmpty(ffmpegPath))
        {
            throw new InvalidOperationException("No \"FFMpegPath\" setting defined for this machine. ffprobe will not be used to collect video metadata.");
        }
        GlobalFFOptions.Configure(options => options.BinaryFolder = ffmpegPath);
    }

}