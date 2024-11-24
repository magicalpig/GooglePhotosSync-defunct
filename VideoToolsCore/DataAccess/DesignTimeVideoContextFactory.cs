using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VideoToolsCore.Services;

namespace VideoToolsCore.DataAccess;



// allows dotnet EF commands to create a VideoContext
public class DesignTimeVideoContextFactory : IDesignTimeDbContextFactory<VideoContext>
{
    private record Settings : IProvidesDbFilePath
    {
        public string? DbFilePath { get; set; }
    }

    public VideoContext CreateDbContext(string[] args)
    {
        var host = HostBuilder.CreateConsoleHost<Settings>(_ => { });
        
        var settings = host.Services.GetRequiredService<IProvidesDbFilePath>();
        var logger = host.Services.GetRequiredService<ILogger<VideoContext>>();

        return new VideoContextFactory(logger, null, settings).CreateVideoContext();
    }
}