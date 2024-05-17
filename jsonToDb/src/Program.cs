// Certain video metadata has to be obtained using the GPTK chrome extension:
// 1. File size, 2. Duration, 3. ID for the batchexecute API (different than the one used by REST API).
// This metadata was scraped using the GPTK extension and saved in JSON files.
// This program reads the JSON files and saves the metadata to the shared database defined by VidCommon.
using System.Text.Json;
using VidCommon;
using VidCommon.Models;

namespace jsonToDb
{
  
  class Program
  {
    private static readonly string SizesFile = "GoogleVideosMetadata.json";
    private static readonly string DurationsFile = "GoogleVideosDurations.json";
    private static readonly string IdMappingsFile = "RestToRpc.json";
    
    private static string? ProjectFolder;
    private const string TableName = "GoogleVideosMetadataFromChrome";
    static void Main(string[] args)
    {
      var db = new VideoContext();
      db.Database.EnsureCreated();


      // we'll be looking in the project root folder for json files
      ProjectFolder = Directory.GetCurrentDirectory();
      if (ProjectFolder is null)
      {
        Console.WriteLine("Could not find the project folder. Exiting.");
        return;
      }
      
      // Load data from the JSON file that has the file sizes      
      string jsonFilePath = Path.Combine(ProjectFolder, SizesFile);
      string jsonData = File.ReadAllText(jsonFilePath);

      // Parse JSON data
      var rawRecords = JsonSerializer.Deserialize<List<dynamic>>(jsonData);
      if (rawRecords is null)
      {
        Console.WriteLine("Could not parse JSON data containing file size results of GPTK calls. Exiting.");
        return;
      }

      // Load data from the JSON file that has the video durations
      string jsonFilePath2 = Path.Combine(ProjectFolder, DurationsFile);
      string jsonData2 = File.ReadAllText(jsonFilePath2);

      // Parse JSON data and save to a dictionary mapping each video's batchexecute/short ID to its duration
      var rawDurations = JsonSerializer.Deserialize<List<List<object>>>(jsonData2);
      var durationsDict = new Dictionary<string, int>();
      if (rawDurations is null)
      {
        Console.WriteLine("Could not parse JSON data containing duration results of GPTK calls. Exiting.");
        return;
      }
      else
      {
        foreach (var duration in rawDurations)
        {
          string vidId = duration[0]?.ToString() ?? "";
          if (!string.IsNullOrEmpty(vidId) && ((JsonElement)duration[1]).TryGetInt32(out int durationMs))
          {
            durationsDict[vidId] = (int)durationMs;
          }
        }
      }

      // Now load JSON that matches a REST ID to a batchexecute RPC ID
      string matchesJsonFilePath = Path.Combine(ProjectFolder, IdMappingsFile);
      string matchesJsonData = File.ReadAllText(matchesJsonFilePath);
      var matches = JsonSerializer.Deserialize<List<List<String>>>(matchesJsonData);
      
      // Convert matches into a dictionary
      var matchesDict = new Dictionary<string, string>();
      if (matches is not null)
      {
        foreach (var match in matches)
        {
          matchesDict[match[1]] = match[0];
        }
      }      

      // This is where the video sizes have been saved to memory
      foreach (var raw in rawRecords)
      {
        var videoInstance = new VideoMetadataFromChrome();
        
        if (raw.GetArrayLength() > 1)
        {                      
          videoInstance.ProductId = raw[0].ToString();
          if (raw[1].GetArrayLength() > 9) 
          { 
            videoInstance.Filesize = raw[1][9].GetInt32();
            videoInstance.Filename = raw[1][3].ToString();
          }
        }               
      
        if (videoInstance.IsValid())
        {
          // attach the ID video uses in the REST API
          if (matchesDict.TryGetValue(videoInstance.ProductId!, out string? value))
          {
            videoInstance.RestId = value;
            Console.WriteLine("Matched video instance with REST API ID. " + videoInstance.RestId);
          }
          else
          {
            Console.WriteLine("Could not find a match for video instance. " + videoInstance.RestId);
          }

          // attach the duration of the video
          if (durationsDict.TryGetValue(videoInstance.ProductId!, out int duration))
          {
            videoInstance.DurationMs = duration;
            Console.WriteLine("Matched video instance with duration. " + videoInstance.DurationMs);
          }
          else
          {
            Console.WriteLine("Could not find a duration for video instance. " + videoInstance.ProductId);
          }
          // Save the video instance to the database
          try
          {
            db.VideoMetadataFromChrome.Add(videoInstance);
            db.SaveChanges();
            Console.WriteLine("Saved video instance to the database. " + videoInstance.ToString());
          }
          catch (Exception e)
          {
            Console.WriteLine("Could not save video instance to the database. " + videoInstance.ToString());
            Console.WriteLine(e.Message);
            // Update the record if it already exists in the database
            var existingRecord = db.VideoMetadataFromChrome.FirstOrDefault(v => v.ProductId == videoInstance.ProductId);
            if (existingRecord != null)
            {
              existingRecord.Filesize = videoInstance.Filesize;
              existingRecord.Filename = videoInstance.Filename;
              existingRecord.RestId = videoInstance.RestId;
              existingRecord.DurationMs = videoInstance.DurationMs;
              db.SaveChanges();
              Console.WriteLine("Updated existing video instance in the database. " + existingRecord.ToString());
            }
          }
        }
        else
        {
          Console.WriteLine("Skipping invalid video instance. " + videoInstance.ToString());
        }           
      }
      
      db.Dispose();
    }
  }
}