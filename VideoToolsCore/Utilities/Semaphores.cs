namespace VideoToolsCore.Utilities;


public static class Semaphores
{
    // Microsoft.WindowsAPICodePack.Shell is not threadsafe so no concurrent access is allowed.
    public static readonly SemaphoreSlim ShellSemaphore = new(1, 1);

    // Allow up to `n` concurrent invocations of ffprobe or ffmpeg executables
    public static readonly SemaphoreSlim FFMpegSemaphore = new(Math.Max(Environment.ProcessorCount - 1, 1));
}