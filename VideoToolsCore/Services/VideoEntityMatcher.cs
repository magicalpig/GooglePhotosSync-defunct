using Microsoft.EntityFrameworkCore;
using VideoToolsCore.DataAccess;
using VideoToolsCore.DataAccess.Models;

namespace VideoToolsCore.Services;

// concerns itself with finding matches between database records
public class VideoEntityMatcher(VideoContextFactory videoContextFactory)
{
    private readonly VideoContextFactory _videoContextFactory = videoContextFactory;
    
    public async Task<VideoFileWindows?> FindMatchingWindowsVideoAsync(VideoFileGoogle g)
    {
        return await _videoContextFactory.CreateUserScopedVideoContext().WindowsVideos
            .FirstOrDefaultAsync(w => 
                EF.Functions.Collate(w.Name, "NOCASE") == g.Name && 
                w.DurationMs.HasValue && 
                g.DurationMs.HasValue && 
                Math.Abs((long)(w.DurationMs - g.DurationMs)) < 2_000);
    }

    public async Task<VideoFileGoogle?> FindMatchingGoogleVideoAsync(VideoFileWindows w)
    {
        return await _videoContextFactory.CreateUserScopedVideoContext().GoogleVideos
            .FirstOrDefaultAsync(g => 
                EF.Functions.Collate(w.Name, "NOCASE") == g.Name && 
                w.DurationMs.HasValue && 
                g.DurationMs.HasValue && 
                Math.Abs((long)(w.DurationMs - g.DurationMs)) < 2_000);
    }
}