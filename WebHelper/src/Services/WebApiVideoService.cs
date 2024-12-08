using VideoToolsCore.DataAccess.Models;
using VideoToolsCore.DataAccess;
using WebHelper.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

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
    private readonly VideoContextFactory _contextFactory = contextFactory;
    private readonly ILogger<WebApiVideoService> _logger = logger;


    public async Task<ConcurrentDictionary<string, UpdateResultCode>> ProcessRpcScrapeAsync(List<RpcScrapedVideo> videos)
    {
        ConcurrentDictionary<string, UpdateResultCode> dbUpdateResults = [];
        ConcurrentBag<VideoFileGoogle> videosToUpdate = [];

        // This loop will collect status codes for each video
        // and keep track of videos which have changes to be committed.
        await Parallel.ForEachAsync(videos, async (inVideo, cancellationToken) =>
        {
            if (string.IsNullOrEmpty(inVideo.RpcId))
            {
                _logger.LogWarning("No RPC ID for video {video}", inVideo);
                return;
            }

            var existing = await GetMatchingGoogleVideo(inVideo);
            if (existing == null)
            {
                _logger.LogDebug("No record found for RpcId {RpcId} or GoogleId {GoogleId}", inVideo.RpcId, inVideo.GoogleId);
                dbUpdateResults.TryAdd(inVideo.RpcId, UpdateResultCode.NotFound);
                return;
            }


            // all videos that were found in the database will at least have their last scan timestamp updated
            videosToUpdate.Add(existing);

            var wasChanged = MergeRpcDataWithGoogleVideo(inVideo, existing);
            if (!wasChanged)
            {
                dbUpdateResults.TryAdd(inVideo.RpcId, UpdateResultCode.NoChange);
            }
        });


        // begin committing changes to db
        using VideoContext db = _contextFactory.CreateUserScopedVideoContext();
        foreach (var video in videosToUpdate)
        {
            if (string.IsNullOrEmpty(video.RpcId))
            {
                continue;
            }

            UpdateResultCode? code = dbUpdateResults.TryGetValue(video.RpcId, out var resultCode) ? resultCode : null;

            try
            {
                db.Attach(video);            
                video.LastMetadataScanTimestamp = DateTime.Now;
                db.Entry(video).State = EntityState.Modified;
                
                await db.SaveChangesAsync();
                // The ones with NoChange only had their last scan timestamp updated.
                // These should keep that result code.
                if (code != UpdateResultCode.NoChange) 
                {
                    dbUpdateResults.TryAdd(video.RpcId, UpdateResultCode.Success);
                }
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating video {video}", video);
                dbUpdateResults.TryAdd(video.RpcId, UpdateResultCode.Failure);
            }
        }
        return dbUpdateResults;
    }

    public async Task<VideoFileGoogle?> GetMatchingGoogleVideo(RpcScrapedVideo inVideo)
    {
        using var db = _contextFactory.CreateUserScopedVideoContext();

        // If a video is coming in with a GoogleId value, that's the REST ID for the video.
        // The web client could only have obtained this by doing a HEAD call using the RPC ID and looking at
        // the redirect response.
        return await db.GoogleVideos.FirstOrDefaultAsync(gv => gv.RpcId == inVideo.RpcId || gv.GoogleId == inVideo.GoogleId);
    }

    public bool MergeRpcDataWithGoogleVideo(RpcScrapedVideo inVideo, VideoFileGoogle existing)
    {
        // compare fields for differences
        bool recordChanged = false;
        foreach (var propName in typeof(RpcScrapedVideo).GetProperties().Select(p => p.Name))
        {
            var inValue = typeof(RpcScrapedVideo)?.GetProperty(propName)?.GetValue(inVideo);
            var oldValue = typeof(VideoFileGoogle)?.GetProperty(propName)?.GetValue(existing);
            if (inValue is not null && !Equals(inValue, oldValue))
            {
                if (propName == "GoogleId" && oldValue != null)
                {
                    _logger.LogDebug("Refusing to change primary key field GoogleId from {existingGoogleId} to {newGoogleId}.", oldValue, inValue);
                    continue;
                }
                _logger.LogDebug("{{{GoogleId}}} Updating {prop} from \"{googleVideoValue}\" to \"{inVideoValue}\"", existing.GoogleId, propName, oldValue, inValue);
                typeof(VideoFileGoogle).GetProperty(propName)?.SetValue(existing, inValue);
                recordChanged = true;
            }
        }
        return recordChanged;
    }

    public async Task<List<string>> GetGoogleVideosWithNoRpcIdASync()
    {
        VideoContext db = _contextFactory.CreateUserScopedVideoContext();
        return await db.GoogleVideos.Where(gv => string.IsNullOrEmpty(gv.RpcId)).Select(gv => gv.GoogleId).ToListAsync();
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