using Microsoft.Extensions.DependencyInjection;

namespace VideoDataAccess.Services;
public class UserScopedVideoServiceFactory(IServiceProvider serviceProvider) : IUserScopedVideoServiceFactory
{
  private readonly IServiceProvider _serviceProvider = serviceProvider;

  // <T> names the type of ICurrentUserService
  public UserScopedVideoService CreateUserScopedVideoService<T>() where T : ICurrentUserService
  {
    var currentUserService = _serviceProvider.GetRequiredService<T>();
    var videoContextFactory = _serviceProvider.GetRequiredService<VideoContextFactory>();
    return new UserScopedVideoService(videoContextFactory, currentUserService);
  }
}

public static class UserScopedVideoServiceFactoryExtensions
{
  public static IServiceCollection AddUserScopedVideoServiceFactory(this IServiceCollection services) 
  {
    services.AddSingleton<VideoContextFactory>();
    services.AddSingleton<IUserScopedVideoServiceFactory, UserScopedVideoServiceFactory>();
    return services;
  }
}