using Microsoft.AspNetCore.Mvc;
using WebHelper.Models;
using WebHelper.Services;

namespace WebHelper.Controllers;
[Route("api/[controller]")]
public class RpcScrapedVideosController(WebApiVideoService videoService, WebApiCurrentUserService currentUserService) : ControllerBase
{
    private readonly WebApiVideoService _videoService = videoService;
    private readonly WebApiCurrentUserService _currentUserService = currentUserService;

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] List<RpcScrapedVideo> videos)
    {        
        // Key is RPC ID. Value is result of update operation.
        var dbUpdateResults = 
            await _videoService.ProcessRpcScrapeAsync(videos);

        // If some matches could not be made, 
        // check whether there are records in database that don't yet have RPC ID.
        List<string> recordsWithNoRpcId = [];
        if (dbUpdateResults.Any(kv => kv.Value == UpdateResultCode.NotFound))
        {
            recordsWithNoRpcId = await _videoService.GetGoogleVideosWithNoRpcIdASync();
        }
        return Ok(new
        {
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
        var user = _currentUserService.UserId;
        return Ok($"Hello from RpcScrapedVideosController, {user}!");
    }
}