using Microsoft.Extensions.DependencyInjection;
using ScraperCommon;
using VideoDataAccess.Services;
using LocalScraper.Services;

namespace LocalScraper;

class Program
{  
  public static async Task Main()
  {
    var host = ScraperHostBuilder.CreateHost(services =>
    {
      // We need the same http client to be used by GoogleScraper and GooglePhotosService
      // so that GoogleScraper can do the login and set the authorization headers for GooglePhotosService to use      
      services.AddSingleton<Scraper>()
        .AddSingleton<LocalComputerCurrentUserService>()
        .AddSingleton<VideoContextFactory>();
    });
 

    var localFileSystemScraper = host.Services.GetRequiredService<Scraper>();
    await localFileSystemScraper.ScrapeVideos();
  }
}

  

  
