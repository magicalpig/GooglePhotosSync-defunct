using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VideoToolsCore.Services.User;

namespace VideoToolsCore.DataAccess;

public interface IProvidesDbFilePath
{
    string? DbFilePath { get; set; }
}

public class VideoContextFactory(ILogger<VideoContext> logger, IServiceProvider? serviceProvider, IProvidesDbFilePath settings)
{
    private readonly ILogger<VideoContext> _logger = logger;
    private readonly IServiceProvider? _serviceProvider = serviceProvider;
    private readonly IProvidesDbFilePath _settings = settings;

    public VideoContext CreateUserScopedVideoContext()
    {
        IDualCurrentUserService? currentUserService = _serviceProvider?.GetRequiredService<IDualCurrentUserService>();

        if (currentUserService is null)
        {
            string errMsg = "Could not create a user-scoped VideoContext because there was no implementation of IDualCurrentUserService available in the service provider.";
            _logger.LogError(errMsg);
            throw new InvalidOperationException(errMsg);
        }

        return new VideoContext(GetConnectionString(), currentUserService);
    }

    internal VideoContext CreateVideoContext()
    {
        return new VideoContext(GetConnectionString(), currentUserService: null);
    }

    private string GetConnectionString()
    {
        if (_settings.DbFilePath is null)
        {
            var settingsJson = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            throw new Exception($"Could not find a db file name under the 'DbFilePath' setting in the Settings section of globalsettings.json.\nSettings looks like {settingsJson}");
        }
        var dbFilePath = _settings.DbFilePath;
            
        if (!Path.IsPathRooted(dbFilePath))
        {
            throw new Exception("The provided db file name must be an absolute path.");
        }

        var connectionString = $"Data Source={dbFilePath}";
        _logger.LogInformation("Database will be stored in: {file}", connectionString);
        return connectionString;
    }
}