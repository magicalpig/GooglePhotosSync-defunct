using VideoToolsCore.Models;
using VideoToolsCore.DataAccess;
using WebHelper.Models;
using Microsoft.EntityFrameworkCore;

namespace WebHelper.Services;
public enum UpdateResultCode
{
  Success,
  Failure,
  NotFound,
  NoChange
}

// Wrapper for VideoContext providing some methods that are specific to receiving video information
// from a web browser that has just made use of Google's RPC API.
public class WebApiVideoService(VideoContextFactory contextFactory, ILogger<WebApiVideoService> logger)
{
  private readonly VideoContext _db = contextFactory.CreateUserScopedVideoContext<WebApiCurrentUserService>();
  private readonly ILogger<WebApiVideoService> _logger = logger;

  // try to update an existing google video record with the metadata just scraped from the RPC API.
  public async Task<UpdateResultCode> UpdateVideoFileGoogle(RpcScrapedVideo inVideo)
  {
    // If a video is coming in with a GoogleId value, that's the REST ID for the video.
    // The client could only have obtained this by doing a HEAD call using the RPC Id and looking at
    // the redirect response.
    // The presence of this property indicates a second attempt to update a _context record for a video
    // scraped from web browser.
    var thisUsersVideos = _db.GoogleVideos;
    VideoFileGoogle? googleVideo = await thisUsersVideos.FirstOrDefaultAsync(gv => gv.RpcId == inVideo.RpcId || gv.GoogleId == inVideo.GoogleId);
    
    if (googleVideo == null)
    {
      _logger.LogDebug("No record found for RpcId {RpcId} or GoogleId {GoogleId}", inVideo.RpcId, inVideo.GoogleId);
      return UpdateResultCode.NotFound;
    }
    else
    {
      // compare fields for differences
      bool recordChanged = false;
      var propsToUpate = new List<string> { "Size", "DurationMs", "GoogleId", "RpcId" };
      foreach (var prop in propsToUpate)
      {
        var inVideoValue = typeof(RpcScrapedVideo)?.GetProperty(prop)?.GetValue(inVideo);
        var googleVideoValue = typeof(VideoFileGoogle)?.GetProperty(prop)?.GetValue(googleVideo);
        if (inVideoValue is not null && !Equals(inVideoValue, googleVideoValue))
        {
          if (prop == "GoogleId" && googleVideoValue != null)
          {
            _logger.LogDebug("Refusing to change primary key field GoogleId from {existingGoogleId} to {newGoogleId}.", googleVideo.GoogleId, inVideoValue);
            continue;
          }          
          _logger.LogDebug("{{{GoogleId}}} Updating {prop} from \"{googleVideoValue}\" to \"{inVideoValue}\"", googleVideo.GoogleId, prop, googleVideoValue, inVideoValue);
          typeof(VideoFileGoogle).GetProperty(prop)?.SetValue(googleVideo, inVideoValue);
          recordChanged = true;
        }
      }

      if (recordChanged)
      {
        try 
        {
          _db.SaveChanges();
          return UpdateResultCode.Success;
        }
        catch (Exception e)
        {
          _logger.LogError(e, "Error updating record");
          return UpdateResultCode.Failure;
        }
      }
      else
      {
        _logger.LogDebug("{{{GoogleId}}} No changes to record", googleVideo.GoogleId);
        return UpdateResultCode.NoChange;
      }
    }
  }

  public async Task<List<string>> GetGoogleVideosWithNoRpcId()
  {
    return await _db.GoogleVideos.Where(gv => string.IsNullOrEmpty(gv.RpcId)).Select(gv => gv.GoogleId).ToListAsync();
  }
}

public static class WebApiVideoServiceExtensions
{
  public static IServiceCollection AddWebApiVideoService(this IServiceCollection services)
  {
    services.AddScoped<WebApiCurrentUserService>();
    services.AddScoped<VideoContextFactory>();
    services.AddScoped<WebApiVideoService>();
    return services;
  }
}