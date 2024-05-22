using Microsoft.EntityFrameworkCore;
using WebHelper.Models;
using VideoDataAccess.Services;
using VideoDataAccess.Models;

namespace WebHelper.Repositories;

public enum UpdateResultCode
{
  Success,
  Failure,
  NotFound,
  NoChange
}
public class VideoRepository(VideoContext context)
{
  private readonly VideoContext db = context;

  // try to update an existing google video record with the metadata just scraped from the RPC API.
  public async Task<UpdateResultCode> UpdateVideoFileGoogle(RpcScrapedVideo inVideo)
  {
    // If a video is coming in with a GoogleId value, that's the REST ID for the video.
    // The client could only have obtained this by doing a HEAD call using the RPC Id and looking at
    // the redirect response.
    // The presence of this property indicates a second attempt to update a db record for a video
    // scraped from web browser.    
    VideoFileGoogle? googleVideo = await db.GoogleVideos.FirstOrDefaultAsync(gv => gv.RpcId == inVideo.RpcId || gv.GoogleId == inVideo.GoogleId);
    
    if (googleVideo == null)
    {
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
          Console.WriteLine($"Updating {prop} from \"{googleVideoValue}\" to \"{inVideoValue}\"");
          typeof(VideoFileGoogle).GetProperty(prop)?.SetValue(googleVideo, inVideoValue);
          recordChanged = true;
        }
      }

      if (recordChanged)
      {
        try 
        {
          await db.SaveChangesAsync();
          return UpdateResultCode.Success;
        }
        catch (Exception e)
        {
          Console.WriteLine($"Error updating record: {e.Message}");
          return UpdateResultCode.Failure;
        }
      }
      else
      {
        return UpdateResultCode.NoChange;
      }
    }
  }

  public async Task<List<string>> GetGoogleVideosWithNoRpcId()
  {
    return await db.GoogleVideos.Where(gv => gv.RpcId == null).Select(gv => gv.GoogleId).ToListAsync();
  }
}