namespace VideoDataAccess.Services;
public interface IUserScopedVideoServiceFactory
{
  UserScopedVideoService CreateUserScopedVideoService<T>() where T : ICurrentUserService;
}