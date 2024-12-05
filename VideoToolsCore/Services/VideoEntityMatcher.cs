using Microsoft.EntityFrameworkCore;
using VideoToolsCore.DataAccess;
using VideoToolsCore.DataAccess.Models;

namespace VideoToolsCore.Services;

// concerns itself with finding matches between database records
public class VideoEntityMatcher(VideoContext dbContext)
{
    private readonly VideoContext _dbContext = dbContext;
    
    public async Task<VideoFileWindows?> FindMatchingWindowsVideoAsync(VideoFileGoogle g)
    {
        return await _dbContext.WindowsVideos
            .FirstOrDefaultAsync(w => 
                EF.Functions.Collate(w.Name, "NOCASE") == g.Name && 
                w.DurationMs.HasValue && 
                g.DurationMs.HasValue && 
                Math.Abs((long)(w.DurationMs - g.DurationMs)) < 2_000);
    }

    public async Task<VideoFileGoogle?> FindMatchingGoogleVideoAsync(VideoFileWindows w)
    {
        return await _dbContext.GoogleVideos
            .FirstOrDefaultAsync(g => 
                EF.Functions.Collate(w.Name, "NOCASE") == g.Name && 
                w.DurationMs.HasValue && 
                g.DurationMs.HasValue && 
                Math.Abs((long)(w.DurationMs - g.DurationMs)) < 2_000);
    }
}