using Microsoft.EntityFrameworkCore;
using VideoToolsCore.DataAccess;
using VideoToolsCore.Models;

namespace VideoToolsCore.Services;

public class VideoMatcher(VideoContext dbContext)
{
    private readonly VideoContext _dbContext = dbContext;
    

    public async Task<VideoFileWindows?> FindMatchingWindowsVideo(VideoFileGoogle g)
    {
        return await _dbContext.WindowsVideos
            .FirstOrDefaultAsync(w => 
                w.Name == g.Name && 
                w.DurationMs.HasValue && 
                g.DurationMs.HasValue && 
                Math.Abs((long)(w.DurationMs - g.DurationMs)) < 2_000);
    }

    public async Task<VideoFileGoogle?> FindMatchingGoogleVideo(VideoFileWindows w)
    {
        return await _dbContext.GoogleVideos
            .FirstOrDefaultAsync(g => 
                w.Name == g.Name && 
                w.DurationMs.HasValue && 
                g.DurationMs.HasValue && 
                Math.Abs((long)(w.DurationMs - g.DurationMs)) < 2_000);
    }
}