using Microsoft.Extensions.DependencyInjection;

namespace VideoDataAccess.Services;
public class UserScopedVideoServiceFactory : IUserScopedVideoServiceFactory
{
  private readonly IServiceProvider _serviceProvider;

  public UserScopedVideoServiceFactory(IServiceProvider serviceProvider)
  {
    _serviceProvider = serviceProvider;
  }

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
    services.AddTransient<GoogleApiCurrentUserService>();
    services.AddTransient<LocalComputerCurrentUserService>();
    services.AddSingleton<IUserScopedVideoServiceFactory, UserScopedVideoServiceFactory>();
    return services;
  }
}