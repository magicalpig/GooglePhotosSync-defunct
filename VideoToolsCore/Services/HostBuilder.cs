using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VideoToolsCore.DataAccess;

namespace VideoToolsCore.Services;

public class HostBuilder
{
    public static WebApplication CreateWebHost<TSettings>(Action<IServiceCollection> configureAdditionalServices) where TSettings : class, new()
    {
        var builder = WebApplication.CreateBuilder();
        ConfigureCommonHost<TSettings>(builder.Host, configureAdditionalServices);
        return builder.Build();
    }

    public static IHost CreateConsoleHost<TSettings>(Action<IServiceCollection> configureAdditionalServices) where TSettings : class, new()
    {
        var builder = Host.CreateDefaultBuilder();
        ConfigureCommonHost<TSettings>(builder, configureAdditionalServices);
        return builder.Build();
    }


    private static void ConfigureCommonHost<TSettings>(IHostBuilder builder, Action<IServiceCollection> configureAdditionalServices) where TSettings : class, new()
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.SetBasePath(AppContext.BaseDirectory).AddJsonFile("globalsettings.json", optional: false, reloadOnChange: true);
        })
        .ConfigureServices((hostContext, services) =>
        {
            var settings = BuildSettingsFromConfig<TSettings>(hostContext.Configuration, hostContext.HostingEnvironment);
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
        });
    }

    private static TSettings BuildSettingsFromConfig<TSettings>(IConfiguration config, IHostEnvironment environment) where TSettings : class, new()
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