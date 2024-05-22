using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace VideoDataAccess.Services;
public class VideoContextFactory(ILogger<VideoContext> logger)
{
  private readonly ILogger<VideoContext> _logger = logger;

  internal VideoContext CreateVideoContext()
  {
    var configuration = new ConfigurationBuilder()
      .AddJsonFile("CommonSettings.json", optional: false, reloadOnChange: true)
      .Build();
      
    var dbFileName = configuration.GetValue<string>("DbFileName") ?? 
      throw new Exception("Could not find a db file name under the 'DbFileName' setting in CommonSettings.json.");

    const int DirectoriesUp = 4;
    var solutionRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
      string.Concat(Enumerable.Repeat("../", DirectoriesUp))));
    var connectionString = $"Data Source={solutionRoot}{dbFileName}";
    _logger.LogInformation("Database will be DbPath: {file}", connectionString);

    return new VideoContext(connectionString);
  }
}