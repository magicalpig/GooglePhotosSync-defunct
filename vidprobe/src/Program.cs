using FFMpegCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VideoDataAccess.Services;
using VideoDataAccess.Models;
using Microsoft.WindowsAPICodePack.Shell;
using VidProbe.Services;

namespace VidProbe;

class Program
{  
  public static async Task Main()
  {
    var host = CreateHost();

    // This gives us a facade for db operations
    var dbFactory = host.Services.GetRequiredService<IUserScopedVideoServiceFactory>();

    var settings = host.Services.GetRequiredService<VidProbeSettings>();
    var logger = host.Services.GetRequiredService<ILogger<Program>>();

    if (false)
    {
      var loginService = host.Services.GetRequiredService<GoogleLoginService>();
      var testResult = await loginService.TestApiEndpoint("https://www.googleapis.com/oauth2/v2/userinfo");
      Console.WriteLine($"TestApiEndpoint: {testResult}"  );
      Environment.Exit(0);
    }
    
    var googleScraper = host.Services.GetRequiredService<GoogleScraper>();
    await googleScraper.ScrapeVideos();

    ScrapeLocalFileSystem(
      dbFactory.CreateUserScopedVideoService<LocalComputerCurrentUserService>(), 
      settings,
      logger
    );
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
          // can't use DI extension method, IServiceCollection.AddGooglePhotos, because it will
          // give the GooglePhotosService a new http client but we need it to share one with GoogleScraper
          .AddSingleton<CasCap.Services.GooglePhotosService>()
          .AddSingleton<GoogleScraper>()
          .AddSingleton<GoogleLoginService>()
          .AddSingleton<LocalComputerCurrentUserService>()
          .AddSingleton<GoogleApiCurrentUserService>()
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

  private static void ScrapeLocalFileSystem(UserScopedVideoService db, VidProbeSettings settings, ILogger<Program> logger)
  {
    logger.LogInformation("Scraping Local File System for videos as user {user}", db.CurrentUser);
    if (settings.FreshDatabase)
    {
      logger.LogInformation("Per the FreshDatabase setting, clearing out any Windows videos from database");
      db.RemoveAllVideos();
    }
    string? sourceDir = settings.SourceDir; 
    if (string.IsNullOrEmpty(sourceDir))
    {
      Console.WriteLine("appsettings.json needs to define SourceDir under the \"Settings\" key.");
      Environment.Exit(1);
    }
    logger.LogInformation("Collecting information about video files in {sourceDir}", sourceDir);

    string? ffmpegPath = settings.FFMpegPath;
    if (ffmpegPath is not null)
    {
      GlobalFFOptions.Configure(options => options.BinaryFolder = ffmpegPath);
    }
    else
    {
      logger.LogWarning("No \"FFMpegPath setting\" defined for this machine. ffprobe will not be used to collect video metadata.");
    }

    // Collect video files from local file system
    string[] filePaths = [];
    if (Directory.Exists(sourceDir))
    {     
      filePaths = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories)
        .Where(f => VideoFileTester.IsVideoFile(Path.GetExtension(f)))
        .Select(f => Path.GetFullPath(f))
        .ToArray();
      logger.LogInformation("Found {count} files on local file system folder {sourceDir}", filePaths.Length, sourceDir);
    }
    else
    {
      logger.LogError("Directory {sourceDir} does not exist.", sourceDir);
    }

    // convert files to records
    List<VideoFileWindows> records = [];    
    // Possibly limit how many local video files are examined/saved into database
    int? maxFilesLocal = settings.FileLimitLocal;
    int i = 0;
    foreach (string filePath in filePaths)
    {
      if (maxFilesLocal > 0 && ++i > maxFilesLocal)
      {
        break;
      }
      string justName = Path.GetFileName(filePath);
      VideoFileWindows videoFile = new()
      {
          Path = filePath,
          Folder = Path.GetDirectoryName(filePath) ?? @"/",
          Name = justName,
          Size = new FileInfo(filePath).Length
      };
  
      
      #region Metadata
      // use ffprobe to get whatever metadata we can
      IMediaAnalysis? mediaInfo = null;
      try { mediaInfo = FFProbe.Analyse(filePath); }
      catch (Exception e) { Console.WriteLine($"Error with ffprobe: {e.Message}"); }

      // also use MicrosoftAPICodePack to get metadata
      string nameForShell = filePath.Replace("/", @"\");
      var shellFile = ShellFile.FromFilePath(nameForShell);
      var propertiesSystem = shellFile.Properties.System;
      
      // Metadata: media creation date
      if (propertiesSystem.Media.DateEncoded.Value != null)
      {        
        videoFile.CreatedDate = propertiesSystem.Media.DateEncoded.Value.Value;
      }
      else
      {
        if (mediaInfo is not null && (mediaInfo.Format?.Tags?.TryGetValue("creation_time", out string? dateString) ?? false))
        {
          videoFile.CreatedDate = DateTime.Parse(dateString);
        }
        else
        {
          videoFile.CreatedDate = File.GetCreationTime(filePath);          
        }
      }
      
      // Metadata: duration
      if (mediaInfo is not null)
      {
        videoFile.DurationMs = (long)mediaInfo.Duration.TotalMilliseconds;
      }
      if (videoFile.DurationMs <= 0)
      {
        // Represents the actual play time of a media file and is measured in 100ns units, not milliseconds.
        videoFile.DurationMs = (long)(propertiesSystem.Media.Duration?.Value / 10_000 ?? 0);
      }

      // Metadata: Title & Comments
      videoFile.Description = string.Join(" | ", new[] { propertiesSystem.Title.Value, propertiesSystem.Comment.Value }.Where(s => !string.IsNullOrEmpty(s)));
      #endregion

      records.Add(videoFile);
      shellFile.Dispose();
    }
    
    records.ForEach(r =>
    {
      VideoFileWindows? existing = db.GetWindowsVideos().FirstOrDefault(w => w.Path == r.Path);
      if (existing is null)
      {
        logger.LogDebug("new video from local file system {filepath}", r.Path);
        db.Add(r);
      }
      else if (!r.Equals(existing))
      {
        logger.LogDebug("updating local video: {filepath}", r.Path);
        existing.SyncTo(r);
        db.Update(existing);
      }
      else
      {
        logger.LogDebug("no change to local video: {filepath}", r.Path);
      }
    });
    db.SaveChanges();
  }    
  public async static Task SomethingElse()
  {  
    GlobalFFOptions.Configure(options => options.BinaryFolder = "c:/ffmpeg/bin");
    string[] filePaths = Directory.GetFiles("d:/homevideos/")
      .Where(f => VideoFileTester.IsVideoFile(Path.GetExtension(f)))
      .ToArray();
    Console.WriteLine($"Found {filePaths.Length} files on local file system");
    
    List<VideoFileWindows> records = [];    

    foreach (string filePath in filePaths)
    {      
      string justName = Path.GetFileName(filePath);
      Console.WriteLine($"Processing {justName}");
      VideoFileWindows videoFile = new()
      {
          Path = filePath,
          Folder = Path.GetDirectoryName(filePath) ?? @"/",
          Name = justName,
          Size = new FileInfo(filePath).Length
      };
  
      string nameForShell = filePath.Replace("/", @"\");        
      using(var shellFile = ShellFile.FromFilePath(nameForShell))
      {
        
        var propertiesSystem = shellFile.Properties.System;
        var mediaCreated = propertiesSystem.Media.DateEncoded.Value;     
        if (mediaCreated != null)
        {
          Console.WriteLine($"    {mediaCreated.Value} from MicrosoftAPICodePack");
          videoFile.CreatedDate = mediaCreated.Value;
        }
        else
        {
          Console.WriteLine($"No media creation date found for {filePath}. Using file creation date.");
          videoFile.CreatedDate = File.GetCreationTime(filePath);
        }
        Console.WriteLine("Seeking Comments");
        string comments = string.Join(" | ", new[] { propertiesSystem.Title.Value, propertiesSystem.Comment.Value }.Where(s => !string.IsNullOrEmpty(s)));
        Console.WriteLine($"    {comments} from MicrosoftAPICodePack propertiesSystem.Comment.Value");        

        records.Add(videoFile);
      }
      
      IMediaAnalysis mediaInfo = await FFProbe.AnalyseAsync(filePath);
      DateTime? fromFFProbe = null;
      if (mediaInfo.Format?.Tags?.TryGetValue("creation_time", out string? dateString) ?? false)
      {
        fromFFProbe = DateTime.Parse(dateString);
      }
      Console.WriteLine($"    {fromFFProbe} from FFProbe");
      if (fromFFProbe != videoFile.CreatedDate)
      {
        Console.WriteLine($"*******FFProbe and MicrosoftAPICodePack disagree on creation date for {filePath}");
      }
    }
    Console.WriteLine("Done with SomethingElse");
  }
}
