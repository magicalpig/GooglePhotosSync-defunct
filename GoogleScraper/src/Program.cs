using Microsoft.Extensions.DependencyInjection;
using GoogleScraper.Services;
using MyHostBuilder = VideoToolsCore.Services.HostBuilder;
using VideoToolsCore.DataAccess;
using GoogleScraper.Auth;
using CasCap.Services;
using Microsoft.Extensions.Logging;
using VideoToolsCore.Services.User;

namespace GoogleScraper;

class Program
{
    public static async Task Main()
    {
        var host = MyHostBuilder.CreateConsoleHost<Settings>(services =>
        {
            services.AddTransient<AuthorizationHandler>();
            services.AddGooglePhotos();
            // need to alter the HttpClient created by AddGooglePhotos to use Authorization header
            services.AddHttpClient<GooglePhotosService>()
                .AddHttpMessageHandler<AuthorizationHandler>();

            services
                .AddSingleton<Scraper>()
                .AddSingleton<VideoContextFactory>()
                .AddSingleton<GoogleLoginService>()
                // This ensures the GoogleApiCurrentUserService is created with the same HttpClient as 
                // used by GooglePhotosService so that the Authorization header will be set.
                .AddSingleton<IGoogleCurrentUserService>(sp =>
                {
                   var clientFactory = sp.GetRequiredService<IHttpClientFactory>();
                   var httpClient = clientFactory.CreateClient(nameof(GooglePhotosService));
                   var logger = sp.GetRequiredService<ILogger<GoogleApiCurrentUserService>>();
                   return new GoogleApiCurrentUserService(httpClient, logger);
                })
                .AddSingleton<IDualCurrentUserService, DualCurrentUserService>();
        });

        var googleScraper = host.Services.GetRequiredService<Scraper>();
        await googleScraper.ScrapeVideos();
        await googleScraper.ScrapeAlbums();
    }
}




