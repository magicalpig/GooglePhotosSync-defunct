using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VideoToolsCore.DataAccess;

namespace VideoToolsCore.Services;

public class HostBuilder
{
    public static IHost CreateHost<TSettings>(Action<IServiceCollection> configureAdditionalServices) where TSettings : class, new()
    {
        // This does a lot of things like loading configuration, logging, setting up a services host.
        // When it loads configuration, one source will be appsettings.{DOTNET_ENVIRONMENT}.json.
        // It will also pull user secrets when in development mode.
        var builder = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                // solution-wide settings
                // The SetBasePath is for the case of running dotnet ef commands.
                // These commands run with the project root dir as the current directory.
                // However globalsettings.json is in the VideoToolsCore/bin/Debug/net8.0-windows or similar dir which is represented by AppContext.BaseDirectory.
                config.SetBasePath(AppContext.BaseDirectory).AddJsonFile("globalsettings.json", optional: false, reloadOnChange: true);
                // project-specific settings will be loaded by convention from appsettings.json files
                Console.WriteLine($"Running from {AppContext.BaseDirectory}");
            })
            .ConfigureServices((hostContext, services) =>
            {
                var config = hostContext.Configuration;
                var settings = BuildSettingsFromConfig<TSettings>(config, hostContext);
                services.AddSingleton(settings);

                if (settings is IProvidesFFMpegPath ffMpegPath)
                {
                    services.AddSingleton<FFMpegHelper>()
                        .AddSingleton(ffMpegPath)
                        .AddSingleton<MetadataScraper>();
                }
                if (settings is IProvidesDbFilePath dbFilePath)
                {
                    services.AddSingleton(dbFilePath);
                }

                configureAdditionalServices(services);
            }
        );
        return builder.Build();
    }

    private static TSettings BuildSettingsFromConfig<TSettings>(IConfiguration config, HostBuilderContext? hostContext)
        where TSettings : class, new()
    {
        if (hostContext is not null && hostContext.HostingEnvironment.IsDevelopment())
        {
            Console.WriteLine($"Current directory: {Environment.CurrentDirectory}");
            var environment = hostContext.HostingEnvironment.EnvironmentName;
            Console.WriteLine($"Current environment: {environment}");
            Console.WriteLine("-----Loaded configuration:-----");
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
        foreach (var section in config.GetSection("MachineSettings").GetChildren())
        {
            if (string.Compare(section.Key, machineName, StringComparison.OrdinalIgnoreCase) == 0)
            {
                section.Bind(settings);
                break;
            }
        }

        return settings;
    }
}