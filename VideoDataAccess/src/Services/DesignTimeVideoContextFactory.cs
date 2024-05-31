using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging;

namespace VideoDataAccess.Services;

// allows dotnet EF commands to create a VideoContext
public class DesignTimeVideoContextFactory : IDesignTimeDbContextFactory<VideoContext>
{
  public VideoContext CreateDbContext(string[] args)
  {
    var logger = new Logger<VideoContext>(new LoggerFactory());
    return new VideoContextFactory(logger, null).CreateVideoContext();
  }
}