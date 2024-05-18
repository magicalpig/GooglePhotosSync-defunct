// Certain video metadata has to be obtained using the GPTK chrome extension:
// 1. File size, 2. Duration, 3. ID for the batchexecute API (different than the one used by REST API).
// This metadata was scraped using the GPTK extension and saved in JSON files.
// This program reads the JSON files and saves the metadata to the shared database defined by VidCommon.
using System.Text.Json;
using VidCommon;
using VidCommon.Models;

namespace jsonToDb;
  
class Program
{
  private static readonly string SizesFile = "GoogleVideosMetadata.json";
  private static readonly string DurationsFile = "GoogleVideosDurations.json";
  private static readonly string IdMappingsFile = "RestToRpc.json";
  private static readonly string ProjectFolder = Directory.GetCurrentDirectory();
  
  static void Main(string[] args)
  {
    var db = new VideoContext();
    db.Database.EnsureCreated();


    // Load data from the JSON file that has the file sizes      
    string jsonFilePathSizes = Path.Combine(ProjectFolder, SizesFile);
    string jsonDataSizes = File.ReadAllText(jsonFilePathSizes);

    // Parse JSON data and save to a dictionary mapping each video's batchexecute/short ID to its file size
    var rawSizes = JsonSerializer.Deserialize<List<dynamic>>(jsonDataSizes);    
    if (rawSizes is null)
    {
      Console.WriteLine("Could not parse JSON data containing file size results of GPTK calls. Exiting.");
      return;
    }
    var sizesDict = new Dictionary<string, int>();
    foreach (var sizeRecord in rawSizes)
    {
      if (sizeRecord.GetArrayLength() > 1 && sizeRecord[1].GetArrayLength() > 9)
      {                      
        sizesDict[sizeRecord[0].GetString()] = sizeRecord[1][9].GetInt32();
      } 
    }


    // Load data from the JSON file that has the video durations
    string jsonFilePathDurations = Path.Combine(ProjectFolder, DurationsFile);
    string jsonDataDurations = File.ReadAllText(jsonFilePathDurations);

    // Parse JSON data and save to a dictionary mapping each video's batchexecute/short ID to its duration
    var rawDurations = JsonSerializer.Deserialize<List<List<object>>>(jsonDataDurations);
    if (rawDurations is null)
    {
      Console.WriteLine("Could not parse JSON data containing duration results of GPTK calls. Exiting.");
      return;
    }
    var durationsDict = new Dictionary<string, int>();
    foreach (var duration in rawDurations)
    {
      string vidId = duration[0]?.ToString() ?? "";
      if (!string.IsNullOrEmpty(vidId) && ((JsonElement)duration[1]).TryGetInt32(out int durationMs))
      {
        durationsDict[vidId] = (int)durationMs;
      }
    }


    // Now load JSON that matches a REST ID to a batchexecute RPC ID
    string jsonFilePathIdMappings = Path.Combine(ProjectFolder, IdMappingsFile);
    string jsonDataIdMappings = File.ReadAllText(jsonFilePathIdMappings);

    // Parse JSON data and save to a dictionary mapping REST ID (primary key in the database) to RPC ID (from batchexecute API
    var rawIdMappings = JsonSerializer.Deserialize<List<List<String>>>(jsonDataIdMappings);
    if (rawIdMappings is null)
    {
      Console.WriteLine("Could not parse JSON data containing ID mappings. Exiting.");
      return;
    }
    var idMappingsDict = new Dictionary<string, string>();
    if (rawIdMappings is not null)
    {
      foreach (var match in rawIdMappings)
      {
        idMappingsDict[match[0]] = match[1];
      }
    }      

    foreach (var restId in idMappingsDict.Keys)
    {
      var videoRecord = db.GoogleVideos.FirstOrDefault(v => v.GoogleId == restId);
      if (videoRecord is null)
      {
        videoRecord = new VideoFileGoogle { GoogleId = restId };
        db.GoogleVideos.Add(videoRecord);
      }
      
      // RPC ID will be used lookup video duration and size
      var rpcId = idMappingsDict[restId];      
      videoRecord.RPCId = rpcId;
      videoRecord.Size = sizesDict.GetValueOrDefault(rpcId);
      videoRecord.DurationMs = durationsDict.GetValueOrDefault(rpcId);
    }
    db.SaveChanges();
    db.Dispose();
  }
}
