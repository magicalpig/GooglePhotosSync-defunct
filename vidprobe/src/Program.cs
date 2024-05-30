using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VideoDataAccess.Services;
using VideoDataAccess.Models;
using VidProbe.Services;

namespace VidProbe;

class Program
{  
  public static async Task Main()
  {
    var host = CreateHost();
    
    if (false)
    {
      var loginService = host.Services.GetRequiredService<GoogleLoginService>();
      var testResult = await loginService.TestApiEndpoint("https://www.googleapis.com/oauth2/v2/userinfo");
      Console.WriteLine($"TestApiEndpoint: {testResult}"  );
      Environment.Exit(0);
    }
    
    var googleScraper = host.Services.GetRequiredService<GoogleScraper>();
    //await googleScraper.ScrapeVideos();

    var localFileSystemScraper = host.Services.GetRequiredService<LocalFileSystemScraper>();
    await localFileSystemScraper.ScrapeVideos();
  }

  private static IHost CreateHost()
  {
    // This does a lot of things like loading configuration, logging, setting up a services host.
    // When it loads configuration, one source will be appsettings.{DOTNET_ENVIRONMENT}.json.
    // It will also pull user secrets when in development mode.
    var builder = Host.CreateDefaultBuilder()
      .ConfigureServices((hostContext, services) =>
      {
        var config = hostContext.Configuration;
        var settings = BuildSettingsFromConfig(config, hostContext);
        
        services
          // this IServiceCollection extension method also registers the dependencies for a UserScopedVideoService
          .AddUserScopedVideoServiceFactory()        
          .AddSingleton(settings)
          // We need the same http client to be used by GoogleScraper and GooglePhotosService
          // so that GoogleScraper can do the login and set the authorization headers for GooglePhotosService to use.        
          .AddSingleton<HttpClient>(
            new HttpClient()
            {
              BaseAddress = new Uri(CasCap.Models.RequestUris.BaseAddress)
            })
          
          .AddSingleton<GoogleScraper>()
          // can't use DI extension method, IServiceCollection.AddGooglePhotos, because it will
          // give the GooglePhotosService a new http client but we need it to share one with GoogleScraper
          .AddSingleton<CasCap.Services.GooglePhotosService>()
          .AddSingleton<GoogleLoginService>()
          .AddSingleton<GoogleApiCurrentUserService>()

          .AddSingleton<LocalFileSystemScraper>()
          .AddSingleton<LocalComputerCurrentUserService>()
        ;
      });
    return builder.Build();
  }

  private static VidProbeSettings BuildSettingsFromConfig(IConfiguration config, HostBuilderContext? hostContext)
  {
    if (hostContext is not null && hostContext.HostingEnvironment.IsDevelopment())
    {
      Console.WriteLine($"Current directory: {Environment.CurrentDirectory}");
      var environment = hostContext.HostingEnvironment.EnvironmentName;
      Console.WriteLine($"Current environment: {environment}");
      Console.WriteLine("Loaded configuration:");
      foreach (var pair in config.AsEnumerable())
      {
        Console.WriteLine($"{pair.Key}: {pair.Value}");
      }
    }
    
    var settings = new VidProbeSettings();
    config.GetSection("Settings").Bind(settings);

    // load machine-specific settings from the "MachineSettings" key of appsettings*.json
    // To allow case-insensitive matching, we'll have to loop over the keys in the MachineSetting section
    string machineName = System.Net.Dns.GetHostName();
    bool foundMachineSpecificSettings = false;
    foreach (var section in config.GetSection("MachineSettings").GetChildren())
    {     
      if (string.Compare(section.Key, machineName, StringComparison.OrdinalIgnoreCase) == 0)
      {
        section.Bind(settings);
        foundMachineSpecificSettings = true;
        break;
      }
    }
    if (!foundMachineSpecificSettings)
    {
      Console.WriteLine($"No MachineSettings defined in appsettings{{Environment}}.json for machine {machineName}");
      Environment.Exit(1);
    }
    return settings;
  }
}
