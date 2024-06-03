using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace VideoDataAccess.Services;
public class VideoContextFactory(ILogger<VideoContext> logger, IServiceProvider? serviceProvider)
{
  private readonly ILogger<VideoContext> _logger = logger;
  private readonly IServiceProvider? _serviceProvider = serviceProvider;

  public VideoContext CreateUserScopedVideoContext<T>() where T : class, ICurrentUserService
  {  
    ICurrentUserService? currentUserService = _serviceProvider?.GetRequiredService<T>();
    
    if (currentUserService is null)
    {
      string errMsgTpl= "Could not create a user-scoped VideoContext using an ICurrentUserService of type {currentUserServiceType}. Maybe that ICurrentUserService implementation is not registered with DI, or maybe no IServiceProvider is available to the VideoContextFactory.";
      _logger.LogError(errMsgTpl, typeof(T));
      throw new InvalidOperationException(string.Format(errMsgTpl, typeof(T)));
    }

    return new VideoContext(GetConnectionString(), currentUserService);
  }

  internal VideoContext CreateVideoContext()
  {
    return new VideoContext(GetConnectionString(), null);
  }

  private string GetConnectionString()
  {
    var configuration = new ConfigurationBuilder()
        .AddJsonFile("DbSettings.json", optional: false, reloadOnChange: true)
        .Build();
        
    var dbFileName = configuration.GetValue<string>("DbFileName") ?? 
      throw new Exception("Could not find a db file name under the 'DbFileName' setting in DbSettings.json.");

    const int DirectoriesUp = 4;
    var solutionRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
      string.Concat(Enumerable.Repeat("../", DirectoriesUp))));
    var connectionString = $"Data Source={solutionRoot}{dbFileName}";
    _logger.LogInformation("Database will be DbPath: {file}", connectionString);
    return connectionString;
  }
}