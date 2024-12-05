using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VideoToolsCore.DataAccess;
using VideoToolsCore.Utilities;
using VideoToolsCore.Metadata.Readers;
using VideoToolsCore.Metadata.Services;
using VideoToolsCore.Metadata.Writers;

namespace VideoToolsCore.Services;

// Sets up DI, Logging, Configuration, and registers services for scraping metadata from local filesystem.
// If creating a web app, you'll also get the benefits of WebApplication.CreateBuilder() such as 
// being able to map endpoints and middleware.
public class HostBuilder
{
    public static WebApplication CreateWebHost<TSettings>(Action<IServiceCollection> configureAdditionalServices) where TSettings : BaseSettings, new()
    {
        var builder = WebApplication.CreateBuilder();
        ConfigureCommonHost<TSettings>(builder.Host, configureAdditionalServices);
        return builder.Build();
    }

    public static IHost CreateConsoleHost<TSettings>(Action<IServiceCollection> configureAdditionalServices) where TSettings : BaseSettings, new()
    {
        var builder = Host.CreateDefaultBuilder();
        ConfigureCommonHost<TSettings>(builder, configureAdditionalServices);
        return builder.Build();
    }


    private static void ConfigureCommonHost<TSettings>(IHostBuilder builder, Action<IServiceCollection> configureAdditionalServices) where TSettings : BaseSettings, new()
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // add globalsettings as the configuration source that comes right after environment variables
            var globalSettingsSource = new JsonConfigurationSource
            {
                Path = Path.Combine(AppContext.BaseDirectory, "globalsettings.json"),
                Optional = false,
                ReloadOnChange = true
            };
            globalSettingsSource.ResolveFileProvider();
            config.Sources.Insert(1, globalSettingsSource);
        })
        .ConfigureServices((hostContext, services) =>
        {
            // register settings as both its base class and as the specific settings class
            // just to satisfy any class that tries to receive it as a BaseSettings
            var settings = BuildSettingsFromConfig<TSettings>(hostContext.Configuration, hostContext.HostingEnvironment);
            services.AddSingleton<BaseSettings>(settings);
            services.AddSingleton(settings);


            // Metadata readers and writers
            services.AddSingleton<IMetadataReader>(sp => new ShellMetadataReader { Priority = 1 });
            services.AddSingleton<IMetadataWriter>(sp => new ShellMetadataWriter { Priority = 1 });
            if (settings is IProvidesFFMpegPath settingsWithFFMpegPath && !string.IsNullOrEmpty(settingsWithFFMpegPath.FFMpegPath))
            {
                // sets the base path for ffmpeg binaries
                FFMpegInitializer.Initialize(settingsWithFFMpegPath);

                services.AddSingleton<IMetadataReader>(sp => new FFMpegMetadataReader { Priority = 2 });
                services.AddSingleton<IMetadataWriter>(sp => new FFMpegMetadataWriter { Priority = 2 });
            }
            // TODO Add taglib or something? As last resort add ffmpeg even though it has to copy the file.

            services.AddSingleton<MetadataHelper>();
            
            if (settings is IProvidesDbFilePath dbFilePath)
            {
                services.AddSingleton(dbFilePath);
            }

            configureAdditionalServices(services);
        });
    }

    private static TSettings BuildSettingsFromConfig<TSettings>(IConfiguration config, IHostEnvironment environment) where TSettings : BaseSettings, new()
    {
        if (environment.IsDevelopment())
        {
            Console.WriteLine($"Current directory: {Environment.CurrentDirectory}");
            var envName = environment.EnvironmentName;
            Console.WriteLine($"Current environment: {envName}");
            Console.WriteLine("-----Loaded configuration:-----");
            foreach (var pair in config.AsEnumerable().OrderBy(pair => pair.Key))
            {
                Console.WriteLine($"{pair.Key}: {pair.Value}");
            }
        }

        var settings = new TSettings();
        config.GetSection("Settings").Bind(settings);

        string machineName = System.Net.Dns.GetHostName();
        foreach (var section in config.GetSection("MachineSettings").GetChildren())
        {
            if (string.Compare(section.Key, machineName, StringComparison.OrdinalIgnoreCase) == 0)
            {
                section.Bind(settings);
                break;
            }
        }

        Console.WriteLine("-----Settings Object:-----");
        Console.WriteLine(JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));

        return settings;
    }
}