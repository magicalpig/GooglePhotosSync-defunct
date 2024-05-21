using FFMpegCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VideoDataAccess;
using VideoDataAccess.Models;
using Microsoft.WindowsAPICodePack.Shell;
using System.Data;

namespace VidProbe;

class Program
{  
  public static async Task Main()
  {
    var host = CreateHost();

    var db = host.Services.GetRequiredService<VideoContext>();
    var config = host.Services.GetRequiredService<IConfiguration>();
    var settings = host.Services.GetRequiredService<Settings>();
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    if (settings.FreshDatabase)
    {
      logger.LogInformation("Per the FreshDatabase setting, clearing out any existing records from Google videos and Windows videos database tables");
      db.WindowsVideos.RemoveRange(db.WindowsVideos);      
      db.GoogleVideos.RemoveRange(db.GoogleVideos);
    }
    
    // Videos from Google
    // Most metadata comes from making calls to the REST API
    // but the file size, duration, and short id come from the jsonToDb project
    //  (which uses a browser-based script to call the batchexecute RPC API)
    var google = host.Services.GetRequiredService<GoogleConnector>();
    
    int? pageLimit = settings.PageLimitGoogle;
    if (pageLimit > 0)
      logger.LogDebug("Collecting information about Google videos up to {pageLimit} pages", pageLimit);
    else
      pageLimit = null;
    
    List<string> newlyScrapedIds = [];
    await foreach (var v in google.ScrapeVideos(pageLimit))
    {
      VideoFileGoogle? existing = db.GoogleVideos.FirstOrDefault(g => g.GoogleId == v.GoogleId);
      if (existing is null)
      {
        logger.LogDebug("new video from Google: {id}", v.GoogleId);
        db.Add(v);
      }
      // possibly update the existing google video database record
      else
      {
        if (!v.Equals(existing))
        {          
          logger.LogDebug("updating google video: {id}", v.GoogleId);
          // copies all possible fields that come back from the REST API into existing record
          // (but leaves other fields, like Size and Duration, alone)
          existing.SyncToRestAPIResult(v);
        }
      } 
    };
    db.SaveChanges();

    // Offer to delete records from database if they weren't found on Google Photos
    // but not if a page limit was used (meaning Google Photos was not completely queried)
    if (pageLimit is null)
    {
      // Check all videos in googleVideos table to see if any were not found by Google Photos API
      var videosInDatabase = db.GoogleVideos.ToList();
      List<VideoFileGoogle> videosNotInGoogle = [];
      foreach (var video in videosInDatabase)
      {
        if (!newlyScrapedIds.Any(id => id == video.GoogleId))
        {
          videosNotInGoogle.Add(video);
        }
      }

      int count = videosNotInGoogle.Count;
      if (count > 0)
      {
        logger.LogDebug("Total cases of video present in db but not on google photos: {count}", count);
        Console.WriteLine("Do you want to delete videosNotInGoogle from the database? (Y/N)");
        string? response = Console.ReadLine();
        if (response?.Equals("Y", StringComparison.OrdinalIgnoreCase) ?? false)
        {
          db.RemoveRange(videosNotInGoogle);
          db.SaveChanges();
          logger.LogInformation("Deleted videosNotInGoogle from the database.");
          Console.WriteLine("List of GoogleIds for records just deleted:");
          foreach (var video in videosNotInGoogle)
          {
              Console.WriteLine(video.GoogleId);
          }
        }
        else
        {
          Console.WriteLine("videosNotInGoogle will not be deleted from the database.");
        }
      }
    }


    
    #region LocalVideoFiles          
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
      VideoFileWindows? existing = db.WindowsVideos.FirstOrDefault(w => w.Path == r.Path);
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
    #endregion    
  }

  private static IHost CreateHost()
  {
    // This does a lot of things like loading configuration, logging, setting up a services host.
    // When it loads configuration, one source will be appsettings.{DOTNET_ENVIRONMENT}.json.
    // It will also pull user secrets when in development mode.
    var builder = Host.CreateDefaultBuilder()
      .ConfigureServices((hostContext, services) =>
      {
        services.AddSingleton<VideoContext>();
        
        var config = hostContext.Configuration;
        var settings = new Settings();
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
          Console.WriteLine($"No MachineSettings defined in appsettings.json for machine {machineName}");
          Environment.Exit(1);
        }
        services.AddSingleton(settings);

        // We need the same http client to be used by GoogleConnector and GooglePhotosService
        // so that GoogleConnector can do the login and set the authorization headers for GooglePhotosService to use.        
        services.AddSingleton<HttpClient>(new HttpClient()
        {
          BaseAddress = new Uri(CasCap.Models.RequestUris.BaseAddress)
        });
        services.AddSingleton<GoogleConnector>();
        // can't use DI extension method, IServiceCollection.AddGooglePhotos, because it will
        // give the GooglePhotosService a new http client but we need it to share one with GoogleConnector
        services.AddSingleton<CasCap.Services.GooglePhotosService>();

      });
    return builder.Build();
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
