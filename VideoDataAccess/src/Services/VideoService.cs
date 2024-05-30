using Microsoft.Extensions.DependencyInjection;
using VideoDataAccess.Models;

// basically a facade for the VideoContext : DbContext allowing access to its
// .Add, .Update, .SaveChanges, and some methods to get datasets that 
// may or may not be filtered down to a specific user id.
namespace VideoDataAccess.Services;
public class VideoService(VideoContextFactory contextFactory) : IVideoService
{
  private readonly VideoContext _context = contextFactory.CreateVideoContext();

  private IQueryable<T> GetVideos<T>(string? userId = null) where T : VideoFileBase
  {
    var results = _context.Set<T>().Where(v => userId == null || v.Owner == userId);
    return results;
  }
  public virtual IQueryable<VideoFileGoogle> GetGoogleVideos(string? userId = null)
  {
    return GetVideos<VideoFileGoogle>(userId);
  }
  public virtual IQueryable<VideoFileWindows> GetWindowsVideos(string? userId = null)
  {
    return GetVideos<VideoFileWindows>(userId);
  }

  public virtual void RemoveRange(IEnumerable<VideoFileBase> videos, string? userId = null)
  {
    //#TODO not sure better to filter down to user's videos only
    // or set user id property on the videos before removing them
    if (userId is not null)
    {
      videos = videos.Where(v => v.Owner == userId);
    }
    _context.RemoveRange(videos);
    _context.SaveChanges();
  }
  public virtual void RemoveAllVideos(string? userId = null)
  {
    var videos = GetVideos<VideoFileBase>(userId);
    _context.RemoveRange(videos);
    _context.SaveChanges();
  }

  public virtual void Add<T>(T video, string? userId = null) where T : VideoFileBase
  {
    if (userId != null)
    {
      video.Owner = userId;
    }
    _context.Add(video);
  }
  
  public virtual void Update<T>(T video, string? userId = null) where T : VideoFileBase
  {
    if (userId != null)
    {
      video.Owner = userId;
    }
    _context.Update(video);
  }

  public void SaveChanges()
  {
    _context.SaveChanges();
  }
} 


// This is how we register the video service into the DI container
// with the client code needing to know what video service's own dependencies are.
public static class VideoServiceExtensions
{
  public static IServiceCollection AddVideoService(this IServiceCollection services)
  {
    services.AddSingleton<VideoContextFactory>();
    services.AddSingleton<IVideoService, VideoService>();
    return services;
  }
}
