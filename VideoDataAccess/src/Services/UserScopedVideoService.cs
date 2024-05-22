using VideoDataAccess.Models;
using Microsoft.Extensions.DependencyInjection;

namespace VideoDataAccess.Services;
public class UserScopedVideoService(VideoContextFactory contextFactory, ICurrentUserService currentUserService)
  : VideoService(contextFactory)
{
  private readonly ICurrentUserService _currentUserService = currentUserService;

  public override IQueryable<VideoFileGoogle> GetGoogleVideos(string? userId = null)
  {
    return base.GetGoogleVideos(userId ?? _currentUserService.UserId);
  }

  public override IQueryable<VideoFileWindows> GetWindowsVideos(string? userId = null)
  {
    return base.GetWindowsVideos(userId ?? _currentUserService.UserId);
  }

  public override void RemoveRange(IEnumerable<VideoFileBase> videos, string? userId = null)
  {
    base.RemoveRange(videos, userId ?? _currentUserService.UserId);
  }

  public override void RemoveAllVideos(string? userId = null)
  {
    base.RemoveAllVideos(userId ?? _currentUserService.UserId);
  }

  public override void Add<T>(T video, string? userId = null)
  {
    base.Add(video, userId ?? _currentUserService.UserId);
  }

  public override void Update<T>(T video, string? userId = null)
  {
    base.Update(video, userId ?? _currentUserService.UserId);
  }
}


// This is how we register the video service into the DI container
// with the client code needing to know what video service's own dependencies are.
public static class UserScopedVideoServiceExtensions
{
  public static IServiceCollection AddUserScopedVideoService<T>(this IServiceCollection services) 
    where T : class, ICurrentUserService
  {
    services.AddSingleton<VideoContextFactory>();
    services.AddSingleton<IVideoService, UserScopedVideoService>();
    services.AddSingleton<ICurrentUserService, T>();    
    return services;
  }
}