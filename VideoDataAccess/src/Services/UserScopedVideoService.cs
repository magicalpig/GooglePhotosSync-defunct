using VideoDataAccess.Models;

namespace VideoDataAccess.Services;
public class UserScopedVideoService(VideoContextFactory contextFactory, ICurrentUserService currentUserService)
  : VideoService(contextFactory)
{
  private readonly ICurrentUserService _currentUserService = currentUserService;

  public override IQueryable<VideoFileGoogle> GetGoogleVideos(string? userId = null)
  {
    return base.GetGoogleVideos(userId ?? CurrentUser);
  }

  public override IQueryable<VideoFileWindows> GetWindowsVideos(string? userId = null)
  {
    return base.GetWindowsVideos(userId ?? CurrentUser);
  }

  public override void RemoveRange(IEnumerable<VideoFileBase> videos, string? userId = null)
  {
    base.RemoveRange(videos, userId ?? CurrentUser);
  }

  public override void RemoveAllVideos(string? userId = null)
  {
    base.RemoveAllVideos(userId ?? CurrentUser);
  }

  public override void Add<T>(T video, string? userId = null)
  {
    base.Add(video, userId ?? CurrentUser);
  }

  public override void Update<T>(T video, string? userId = null)
  {
    base.Update(video, userId ?? CurrentUser);
  }


  // let client code see what user this service is scoped to
  public string CurrentUser
  {
    get => _currentUserService.UserId;
  }
}


