using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VideoToolsCore.Services;
using VideoToolsCore.DataAccess;
using MetadataGoogleToWindows.Services;

namespace MetadataGoogleToWindows;

class Program
{
    public static async Task Main()
    {
        var host = HostBuilder.CreateConsoleHost<Settings>(services =>
        {
            // We need the same http client to be used by GoogleScraper and GooglePhotosService
            // so that GoogleScraper can do the login and set the authorization headers for GooglePhotosService to use      
            services.AddSingleton<MetadataCopier>()
                .AddSingleton<LocalComputerCurrentUserService>()
                .AddSingleton<VideoContextFactory>()
                .AddSingleton<FFMpegHelper>()
                .AddSingleton<ILocalCurrentUserService, LocalComputerCurrentUserService>()
                .AddSingleton<IDualCurrentUserService, DualCurrentUserService>()
                // puts a User-Scoped Video Context into DI container
                .AddSingleton(sp => {
                    var factory = sp.GetRequiredService<VideoContextFactory>();
                    return factory.CreateUserScopedVideoContext();

                });
        });

        var metadataCopier = host.Services.GetRequiredService<MetadataCopier>();
        try
        {
            var settings = host.Services.GetRequiredService<Settings>();
            var sourceDir = settings.GetRequiredSetting("SourceDir");
            var propToCopy = settings.GetRequiredSetting("MetadataProperty");
            var googleOwner = settings.GetRequiredSetting("GoogleOwner");
            await metadataCopier.CopyMetadata(sourceDir, propToCopy, googleOwner);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while copying metadata from Google videos to Windows videos.");
        }
    }
}