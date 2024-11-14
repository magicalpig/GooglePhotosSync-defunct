using Microsoft.Extensions.DependencyInjection;
using ScraperCommon;
using VideoDataAccess.Services;
using LocalScraper.Services;
using Microsoft.Extensions.Logging;

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
        .AddSingleton<VideoContextFactory>()
        .AddSingleton<FFMpegHelper>();
    });
 

    var localFileSystemScraper = host.Services.GetRequiredService<Scraper>();
    try 
    {
        await localFileSystemScraper.ScrapeVideos();
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while scraping videos.");
    }

  }
}

  

  
