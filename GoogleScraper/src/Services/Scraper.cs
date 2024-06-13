using Microsoft.EntityFrameworkCore;
using CasCap.Models;
using CasCap.Services;
using VideoDataAccess.Models;
using Microsoft.Extensions.Logging;
using VideoDataAccess.Services;
using ScraperCommon;

namespace GoogleScraper.Services;

public class Scraper(VideoContextFactory dbFactory, Settings settings, ILogger<Scraper> logger, GooglePhotosService service)
{
  // Injected dependencies
  private readonly Settings _settings = settings;
  private readonly ILogger<Scraper> _logger = logger;
  private readonly GooglePhotosService _googleService = service;
  private readonly VideoContext _db = dbFactory.CreateUserScopedVideoContext<GoogleApiCurrentUserService>();
  
  private static readonly Filter _videoOnlyFilter = new()
  {
    mediaTypeFilter = new() { mediaTypes = [GooglePhotosMediaType.VIDEO] }
  };

  public async Task ScrapeAlbums() 
  {
    if (_settings.ScrapeGoogleAlbums is false)
    {
      _logger.LogInformation("ScrapeGoogleAlbums setting is false, so not scraping Google Photos albums from REST API");
      return;
    }
    _logger.LogInformation("Scraping Google Photos albums as user {user}", _db.GetCurrentUser() ?? "unknown");    
    
    // get most recent list of albums as well as the existing list for comparison
    List<Album> albums = await _googleService.GetAlbumsAsync();
    List<GoogleAlbum> existingAlbums = await _db.GoogleAlbums.ToListAsync();
    albums.ForEach(a => 
    {
      var existing = existingAlbums.FirstOrDefault(ea => ea.Id == a.id);
      if (existing is not null)
      {
        _logger.LogDebug("Album {title} already in database", existing.Title);
        if (existing.Title != a.title || existing.ItemCount != a.mediaItemsCount)        
        {
          _logger.LogDebug("Updating album {title} in database", existing.Title);
          existing.Title = a.title;
          existing.ItemCount = a.mediaItemsCount;
        }
        else
        {
          _logger.LogDebug("No changes to album {title} in database", a.title);
        }
      }
      else
      {
        _db.Add(new GoogleAlbum() 
          { 
            Id = a.id,
            Title = a.title,
            ItemCount = a.mediaItemsCount
          }
        );
        _logger.LogDebug("Added album {title} to database", a.title);
      }
    });

    var albumsToDelete = existingAlbums.Where(ea => !albums.Any(a => a.id == ea.Id));
    foreach (var albumToDelete in albumsToDelete)
    {
      _db.GoogleAlbums.Remove(albumToDelete);
      _logger.LogInformation("Removed album {title} from database", albumToDelete.Title);
    }
    
    await _db.SaveChangesAsync();
    _logger.LogDebug("Saved changes to albums table");
    existingAlbums = null!; // free up memory
    

    // Now for the video-to-album membership
    List<AlbumMembership> existingAlbumMemberships = await _db.AlbumMemberships.ToListAsync();
    IAsyncEnumerable<AlbumMembership> albumMemberships = ScrapeAlbumMemberships(albums.Select(a => a.id).Reverse());
    
    await foreach (var m in albumMemberships)
    {
      _logger.LogDebug("An album membership was returned at {timestamp}", DateTime.UtcNow);
      var existing = existingAlbumMemberships.FirstOrDefault(e => e.GoogleAlbumId == m.GoogleAlbumId && e.VideoFileGoogleId == m.VideoFileGoogleId);
      if (existing is null)
      {
        _db.Add(m);
      }
      else
      {
        existing.FoundInLatestScrape = true;
      }
    };

    var membershipsToDelete = existingAlbumMemberships.Where(e => !e.FoundInLatestScrape);
    _db.RemoveRange(membershipsToDelete);

    await _db.SaveChangesAsync();
  }

  private async IAsyncEnumerable<AlbumMembership> ScrapeAlbumMemberships(IEnumerable<string> albumIds)
  {
    // want to stay below 10 queries per second
    Queue<DateTime> queryTimes = new();
    int pageSize = 100;
    foreach (var albumId in albumIds)
    {
      _logger.LogDebug("Scraping album {albumId}", albumId);
      // Google's API does not allow setting of any filters in conjuction with specifying an album id
      IAsyncEnumerable<MediaItem> mediaItems = _googleService.GetMediaItemsByAlbumAsync(albumId, pageSize);
      _logger.LogDebug("Got media items for album {albumId}", albumId);
      int itemCountThisCall = 0;
      await foreach(var mediaItem in mediaItems)
      {
        itemCountThisCall++;
     
        if (mediaItem.isVideo)
        {     
          yield return new AlbumMembership(mediaItem.id, albumId);
        }
      }
      
      int apiCallsMadeThisCall = (int)Math.Ceiling((double)itemCountThisCall / pageSize);
      _logger.LogDebug("Made {apiCallsMadeThisCall} API calls to get all items in album", apiCallsMadeThisCall);
      for (int i = 0; i < apiCallsMadeThisCall; i++)
      {
        queryTimes.Enqueue(DateTime.UtcNow);
        // Remove timestamps more than 1 second old
        while(queryTimes.Count > 0 && (DateTime.UtcNow - queryTimes.Peek()).TotalSeconds > 1)
        {
          queryTimes.Dequeue();
        }
        _logger.LogDebug("Count of api calls made in last second: {count}", queryTimes.Count);
        // Don't allow 10 queries to be made in 1 second
        if (queryTimes.Count > 7)
        {
          _logger.LogDebug("Sleeping for 2 seconds to avoid rate limiting");
          await Task.Delay(2000);
        }
      }
    }
  }

  // Most metadata comes from making calls to the REST API
  // but the file size, duration, and short id come from the jsonToDb project
  //  (which uses a browser-based script to call the batchexecute RPC API)
  public async Task ScrapeVideos() 
  {
    if (_settings.ScrapeGoogleVideos is false)
    {
      _logger.LogInformation("ScrapeGoogleVideos setting is false, so not scraping Google Photos from REST API");
      return;
    }
        
    if (_settings.FreshDatabase)
    {
      _logger.LogInformation("Per the FreshDatabase setting, clearing out any Google videos from database");
      _db.RemoveAllVideos();
    }
    
    int? pageLimit = _settings.PageLimitGoogle;
    if (pageLimit > 0)
      _logger.LogDebug("Collecting information about Google videos up to {pageLimit} pages for user {localUserId}", pageLimit, _db.GetCurrentUser() ?? "unknown");
    else
      pageLimit = null;


    IAsyncEnumerable<MediaItem> mediaItems =
        _googleService.GetMediaItemsByFilterAsync(_videoOnlyFilter, pageLimit ?? int.MaxValue);    
    List<string> idsScraped = await ProcessMediaItems(mediaItems);
    _db.SaveChanges();

    // Offer to delete records from database that were not found in this scraping
    // but only if full scrape (no page limit) was done
    if (pageLimit is null)
    {
      OfferDelete(idsScraped);
    }
  }


  private void OfferDelete(List<string> idsScraped)
  {    
    var videosInDatabase = _db.GoogleVideos.ToList();
    List<VideoFileGoogle> videosNotInGoogle = [];
    foreach (var video in videosInDatabase)
    {
      if (!idsScraped.Any(id => id == video.GoogleId))
      {
        videosNotInGoogle.Add(video);
      }
    }

    int count = videosNotInGoogle.Count;
    if (count > 0)
    {
      _logger.LogDebug("Total cases of video present in db but not on google photos: {count}", count);
      _logger.LogDebug("List of GoogleIds for records not found in latest scrape:");
      foreach (var video in videosNotInGoogle)
      {
        _logger.LogDebug("{videoId}", video.GoogleId);
      }
      Console.WriteLine("Do you want to delete videosNotInGoogle from the database? (Y/N)");
      string? response = Console.ReadLine();
      if (response?.Equals("Y", StringComparison.OrdinalIgnoreCase) ?? false)
      {
        _db.RemoveRange(videosNotInGoogle);
        _db.SaveChanges();
        _logger.LogInformation("Deleted videosNotInGoogle from the database.");
        _logger.LogInformation("List of GoogleIds for records just deleted:");
        foreach (var video in videosNotInGoogle)
        {
            _logger.LogInformation("{videoId}", video.GoogleId);
        }
      }
      else
      {
        _logger.LogInformation("videosNotInGoogle will not be deleted from the database.");
      }
    }
  }

  // Adds or updates videos in the db context to later be committed.
  // returns list of newly scraped ids
  private async Task<List<string>> ProcessMediaItems(IAsyncEnumerable<MediaItem> mediaItems)
  {
    List<string> newlyScrapedIds = [];
    await foreach(var m in mediaItems) 
    {
      _logger.LogDebug("Got media item {mediaItemId} from Google", m.id);
      var v = new VideoFileGoogle()
      {
        GoogleId = m.id,
        Name = m.filename,
        CreatedDate = m.mediaMetadata.creationTime.ToLocalTime(),
        Description = m.description,
      };
      newlyScrapedIds.Add(v.GoogleId);

      // process the video
      // add to db if new
      // update in db if exist
      VideoFileGoogle? existing = await _db.GoogleVideos.FirstOrDefaultAsync(g => g.GoogleId == v.GoogleId);
      if (existing is null)
      {
        _logger.LogDebug("new video from Google: {id}", v.GoogleId);
        _db.Add(v);
      }
      // possibly update the existing google video database record
      else
      {
        // duration, size, and url are all going to be null from the REST API
        if (VideoFileGoogle.IsRestApiMetadataDifferent(v, existing))
        {          
          _logger.LogDebug("updating google video: {id}", v.GoogleId);
          // copies all possible fields that come back from the REST API into existing record
          // (but leaves other fields, like Size and Duration, alone)
          existing.SyncToRestAPIResult(v);
        }
      }
    }
    return newlyScrapedIds;
  }

}