using Microsoft.EntityFrameworkCore;
using CasCap.Models;
using CasCap.Services;
using VideoDataAccess.Models;
using Microsoft.Extensions.Logging;
using VideoDataAccess.Services;
using ScraperCommon;

namespace GoogleScraper.Services;
//'The provider for the source IQueryable doesn't implement IDbAsyncQueryProvider. Only providers that implement IDbAsyncQueryProvider can be used for Entity Framework asynchronous operations
public class Scraper(VideoContextFactory dbFactory, Settings settings, ILogger<Scraper> logger, GooglePhotosService service, GoogleLoginService googleLoginService)
{
  // Injected dependencies
  private readonly Settings _settings = settings;
  private readonly ILogger<Scraper> _logger = logger;
  private readonly GooglePhotosService _googleService = service;
  private readonly GoogleLoginService _googleLoginService = googleLoginService;
  private readonly VideoContext _db = dbFactory.CreateUserScopedVideoContext<GoogleApiCurrentUserService>();
    
  private static readonly Filter _videoOnlyFilter = new()
  {
    mediaTypeFilter = new() { mediaTypes = [GooglePhotosMediaType.VIDEO] }
  };

  

  // Most metadata comes from making calls to the REST API
  // but the file size, duration, and short id come from the jsonToDb project
  //  (which uses a browser-based script to call the batchexecute RPC API)
  public async Task ScrapeVideos() {
    await _googleLoginService.LoginAsync();
        
    if (_settings.FreshDatabase)
    {
      _logger.LogInformation("Per the FreshDatabase setting, clearing out any Google videos from database");
      _db.RemoveAllVideos();
    }
    
    int? pageLimit = _settings.PageLimitGoogle;
    if (pageLimit > 0)
      _logger.LogDebug("Collecting information about Google videos up to {pageLimit} pages for user {localUserId}", pageLimit, _db.GetCurrentUser() ?? "<no user set>");
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
    var videosInDatabase = _db.GetGoogleVideos().ToList();
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
      Console.WriteLine("Do you want to delete videosNotInGoogle from the database? (Y/N)");
      string? response = Console.ReadLine();
      if (response?.Equals("Y", StringComparison.OrdinalIgnoreCase) ?? false)
      {
        _db.RemoveRange(videosNotInGoogle);
        _db.SaveChanges();
        _logger.LogInformation("Deleted videosNotInGoogle from the database.");
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
      VideoFileGoogle? existing = await _db.GetGoogleVideos().FirstOrDefaultAsync(g => g.GoogleId == v.GoogleId);
      if (existing is null)
      {
        _logger.LogDebug("new video from Google: {id}", v.GoogleId);
        _db.Add(v);
      }
      // possibly update the existing google video database record
      else
      {
        if (!v.Equals(existing))
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