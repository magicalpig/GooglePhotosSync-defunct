using FFMpegCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using VidCommon;
using VidCommon.Models;
using Microsoft.WindowsAPICodePack.Shell;
using System.Data;
using VidProbe.Models;

namespace VidProbe;


class Program
{  
  public static async Task Main()
  {
//    await SomethingElse();
//    Environment.Exit(0);
    // initialize db connection
    var db = new VideoContext();
    
    // This does a lot of things like loading configuration, logging, setting up a services host.
    // When it loads configuration, one source will be appsettings.{DOTNET_ENVIRONMENT}.json.
    // It will also pull user secrets when in development mode.
    // See: https://learn.microsoft.com/en-us/dotnet/core/extensions/generic-host?tabs=appbuilder
    var builder = Host.CreateApplicationBuilder();
    var config = builder.Configuration;
    var settings = config.GetSection("Settings");

    // load machine-specific settings from the "MachineSettings" key of appsettings.json    
    string machineName = Environment.MachineName;
    MachineSettings? currentMachineSettings = null;
    foreach (var section in config.GetSection("MachineSettings").GetChildren())
    {
      string n = section.Key[..15]; // machineName from Environment.MachineName is max 15 characters
      if (n.Equals(machineName, StringComparison.OrdinalIgnoreCase))
      {
        currentMachineSettings = new MachineSettings();
        config.GetSection($"MachineSettings:{section.Key}").Bind(currentMachineSettings);
        break;
      }
    }
    
    if (currentMachineSettings is null)
    {
      Console.WriteLine($"No MachineSettings defined in appsettings.json for machine {Environment.MachineName}");
      Environment.Exit(1);
    }

    string? sourceDir = currentMachineSettings.SourceDir; 
    if (sourceDir is null)
    {
      Console.WriteLine("appsettings.json needs to define SourceDir under the \"Settings\" key.");
      Environment.Exit(1);
    }
  
    if (settings.GetValue<Boolean>("FreshDatabase"))
    {
      Console.WriteLine("Clearing out any existing records from database");
      db.WindowsVideos.RemoveRange(db.WindowsVideos);
      db.GoogleVideos.RemoveRange(db.GoogleVideos);
    }
    
    // Videos from Google
    // Most metadata comes from making calls to the REST API
     // but the file size, duration, and short id come from a local database (googleFileSizes.db) populated
    // by the jsonToDb project (which uses a browser-based script to call the batchexecute RPC API)
    var google = new GoogleConnector(config, db);
    
    int? pageLimit = Int32.TryParse(settings["PageLimitGoogle"], out int parsedPageLimit) && parsedPageLimit > 0 ? parsedPageLimit : null;
    Console.WriteLine($"Collecting information about Google videos up to {pageLimit} pages");
    List<VideoFileGoogle> googleVideos = await google.ScrapeVideos(pageLimit);
    googleVideos.ForEach(v =>
    {
      VideoFileGoogle? existing = db.GoogleVideos.FirstOrDefault(g => g.GoogleId == v.GoogleId);
      if (existing is null)
      {
        Console.WriteLine($"new video from Google: {v.GoogleId}");
        db.Add(v);
      }
      // possibly update the existing google video database record
      else
      {
        if (!v.Equals(existing))
        {          
          Console.WriteLine($"updating google video: {v.GoogleId}");
          existing.SyncTo(v);
          db.Update(existing);
        }
      } 
    });
    db.SaveChanges();

    // Offer to delete records from database if they weren't found on Google Photos
    // but not if a page limit was used (meaning Google Photos was not completely queried)
    if (pageLimit is null)
    {
      // Check all videos in googleVideos table to see if any were not found by Google Photos API
      var videosInDatabase = db.GoogleVideos.ToList();
      List<VideoFileGoogle> videosNotInGoogle = new List<VideoFileGoogle>();
      foreach (var video in videosInDatabase)
      {
        if (!googleVideos.Any(v => v.GoogleId == video.GoogleId))
        {
          videosNotInGoogle.Add(video);
        }
      }

      int count = videosNotInGoogle.Count;
      if (count > 0)
      {
        Console.WriteLine($"Total cases of video present in db but not on google photos: {count}");
        Console.WriteLine("Do you want to delete videosNotInGoogle from the database? (Y/N)");
        string? response = Console.ReadLine();
        if (response?.Equals("Y", StringComparison.OrdinalIgnoreCase) ?? false)
        {
          db.RemoveRange(videosNotInGoogle);
          db.SaveChanges();
          Console.WriteLine("Deleted videosNotInGoogle from the database.");
          Console.WriteLine("List of GoogleIds for deleted records:");
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
    Console.WriteLine($"Collecting information about video files in {sourceDir}");

    string? ffmpegPath = currentMachineSettings.FFMpegPath;
    if (ffmpegPath is not null)
    {
      GlobalFFOptions.Configure(options => options.BinaryFolder = ffmpegPath);
    }
    else
    {
      Console.WriteLine("No \"FFMpegPath setting\" defined for this machine. ffprobe will not be used to collect video metadata.");
    }


    // Collect video files from local file system
    string[] filePaths = [];
    if (Directory.Exists(sourceDir))
    {     
      filePaths = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories)
        .Where(f => VideoFileTester.IsVideoFile(Path.GetExtension(f)))
        .Select(f => Path.GetFullPath(f))
        .ToArray();
      Console.WriteLine($"Found {filePaths.Length} files on local file system folder {sourceDir}");      
    }
    else
    {
      Console.WriteLine($"Directory {sourceDir} does not exist.");
    }

    // convert files to records
    List<VideoFileWindows> records = [];    
    // Possibly limit how many local video files are examined/saved into database
    int maxFilesLocal = settings.GetValue<int>("FileLimitLocal");
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
        Console.WriteLine($"new video from local file system {r.Path}");
        db.Add(r);
      }
      else if (!r.Equals(existing))
      {
        Console.WriteLine($"updating local video: {r.Path}");
        existing.SyncTo(r);
        db.Update(existing);
      }
      else
      {
        Console.WriteLine($"no change to local video: {r.Path}");
      }
    });
    db.SaveChanges();
    #endregion    
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
