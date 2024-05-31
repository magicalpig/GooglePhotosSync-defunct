using Microsoft.AspNetCore.Mvc;
using WebHelper.Models;
using WebHelper.Services;

namespace WebHelper.Controllers;
[Route("api/[controller]")]
public class RpcScrapedVideosController(WebApiVideoService videoService) : ControllerBase
{
  private readonly WebApiVideoService _db = videoService;
  [HttpPost]
  public async Task<IActionResult> Post([FromBody] List<RpcScrapedVideo> videos)
  {
    // Key is RPC ID. Value is result of update operation.
    Dictionary<string, UpdateResultCode> dbUpdateResults = [];
    foreach (var video in videos)
    {
      var result = await _db.UpdateVideoFileGoogle(video);
      dbUpdateResults.Add(video.RpcId, result);
    }
    
    // If some matches could not be made, 
    // check whether there are records in database that don't yet have RPC ID.
    List<string> recordsWithNoRpcId = [];
    if (dbUpdateResults.Any(kv => kv.Value == UpdateResultCode.NotFound))
    {
      recordsWithNoRpcId = await _db.GetGoogleVideosWithNoRpcId();
    }
    return Ok(new {
      videosReceived = videos.Count,
      recordsFoundAndUpdated = dbUpdateResults.Where(kv => kv.Value == UpdateResultCode.Success)
        .Select(kv => kv.Key)
        .ToList(),
      recordsNotFound = dbUpdateResults.Where(kv => kv.Value == UpdateResultCode.NotFound)
        .Select(kv => kv.Key)
        .ToList(),
      recordsAlreadyUpToDate = dbUpdateResults.Where(kv => kv.Value == UpdateResultCode.NoChange)
        .Select(kv => kv.Key)
        .ToList(),
      recordsFailedToUpdate = dbUpdateResults.Where(kv => kv.Value == UpdateResultCode.Failure)
        .Select(kv => kv.Key)
        .ToList(),
      recordsWithNoRpcId
    });
  }
  
  [HttpGet]
  public IActionResult Get()
  {
    return Ok("Hello from SendScrapedVideosController!");
  }
}