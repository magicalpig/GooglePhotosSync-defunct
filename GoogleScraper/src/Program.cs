using Microsoft.Extensions.DependencyInjection;
using ScraperCommon;
using GoogleScraper.Services;
using VideoDataAccess.Services;

namespace GoogleScraper;

class Program
{  
  public static async Task Main()
  {
    var host = ScraperHostBuilder.CreateHost(services =>
    {
      // We need the same http client to be used by GoogleScraper and GooglePhotosService
      // so that GoogleScraper can do the login and set the authorization headers for GooglePhotosService to use      
      services.AddSingleton<HttpClient>(
        new HttpClient()
        {
          BaseAddress = new Uri(CasCap.Models.RequestUris.BaseAddress)
        })
        .AddSingleton<Scraper>()
        .AddSingleton<VideoContextFactory>()
        // can't use DI extension method, IServiceCollection.AddGooglePhotos, because it will
        // give the GooglePhotosService a new http client but we need it to share one with  GoogleScraper
        .AddSingleton<CasCap.Services.GooglePhotosService>()
        .AddSingleton<GoogleLoginService>()
        .AddSingleton<GoogleApiCurrentUserService>();
    });
 
    var googleScraper = host.Services.GetRequiredService<Scraper>();
    await googleScraper.ScrapeVideos();
  }
}

  

  
