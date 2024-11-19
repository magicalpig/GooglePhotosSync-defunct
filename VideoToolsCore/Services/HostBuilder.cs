using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace VideoToolsCore.Services;

public class HostBuilder
{
    public static IHost CreateHost<TSettings>(Action<IServiceCollection> configureAdditionalServices) where TSettings : class, new()
    {
        // This does a lot of things like loading configuration, logging, setting up a services host.
        // When it loads configuration, one source will be appsettings.{DOTNET_ENVIRONMENT}.json.
        // It will also pull user secrets when in development mode.
        var builder = Host.CreateDefaultBuilder()
          .ConfigureServices((hostContext, services) =>
          {
              var config = hostContext.Configuration;
              var settings = BuildSettingsFromConfig<TSettings>(config, hostContext);
              services.AddSingleton(settings);
              if (settings is IProvidesFFMpegPath ffmpegSettings)
              {
                  services.AddSingleton<IProvidesFFMpegPath>(ffmpegSettings);
              }

              configureAdditionalServices(services);
          }
        );
        return builder.Build();
    }

    private static TSettings BuildSettingsFromConfig<TSettings>(IConfiguration config, HostBuilderContext? hostContext) where TSettings : class, new()
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

        var settings = new TSettings();
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