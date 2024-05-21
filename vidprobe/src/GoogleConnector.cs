// This gets a list of videos from Google Photos using the Google Photos REST API.
// It also queries a SQLite database to get video sizes and durations
// which were recorded in a previous run of the jsonToDb project.
// The only way to get size and duration is to use the batchexecute RPC API, 
// which I did directly in Chrome. I saved the results and fed them into jsonToDb so they could
// be saved in a SQLite database.
using Microsoft.Extensions.Logging;
using CasCap.Models;
using CasCap.Services;
using VideoDataAccess;
using VideoDataAccess.Models;
using Google.Apis.Auth.OAuth2;
using System.Net.Http.Headers;

namespace VidProbe;

public class GoogleConnector(Settings settings, VideoContext dbIn, ILogger<GoogleConnector> logger, HttpClient sharedClient, GooglePhotosService service)
{  
  private static readonly Filter _videoOnlyFilter = new()
  {
    mediaTypeFilter = new() { mediaTypes = [GooglePhotosMediaType.VIDEO] }
  };

  private readonly VideoContext _db = dbIn;
  private readonly Settings _settings = settings;
  private readonly ILogger<GoogleConnector> _logger = logger;
  private readonly HttpClient _sharedClient = sharedClient;
  private readonly GooglePhotosService _service = service;

  // not using CasCap's login method because I get an exception, probably having to do with the fact that
  // I'm omitting the client secret in my token request. 
  private async Task<bool> Login()
  {    
    _logger.LogDebug("Logging in to Google API with client ID {clientId}", _settings.GoogleAPIClientId);
    UserCredential credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
      new ClientSecrets { ClientId = _settings.GoogleAPIClientId },
      [ "https://www.googleapis.com/auth/photoslibrary.readonly" ],
      "googiegoogie",
      CancellationToken.None
    );

    _logger.LogInformation("Access Token: {accessToken}", credential.Token.AccessToken);
    _logger.LogInformation("Refresh Token: {refreshToken}", credential.Token.RefreshToken);
    _logger.LogInformation("Expiration Time: {expiration} seconds", credential.Token.ExpiresInSeconds);

    if (credential.Token.IsStale)
    {
      _logger.LogWarning("The access token has expired, refreshing it");
      if (await credential.RefreshTokenAsync(CancellationToken.None))
      {
          _logger.LogInformation("The access token is now refreshed");
      }
      else
      {
          _logger.LogError("The access token has expired but we can't refresh it :(");
          return false;
      }
    }
    else
    {
      _logger.LogDebug("The access token is OK, continue");
    }
    
    _sharedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(credential.Token.TokenType, credential.Token.AccessToken);
    return true;
  }

  public async IAsyncEnumerable<VideoFileGoogle> ScrapeVideos(int? pageLimit) {
    List<VideoFileGoogle> videos = [];
        
    var loginSuccess = await Login();
    IAsyncEnumerable<MediaItem> mediaItems =
      _service.GetMediaItemsByFilterAsync(_videoOnlyFilter, pageLimit ?? int.MaxValue);
    
    await foreach(var m in mediaItems) 
    {
      _logger.LogDebug("Processing Google media item {mediaItemId}", m.id);
      var v = new VideoFileGoogle(){
        GoogleId = m.id,
        Name = m.filename,
        CreatedDate = m.mediaMetadata.creationTime.ToLocalTime(),
        Description = m.description,
      };
      yield return v;
      _logger.LogDebug("Returned video from scraper: {googleId}", v.GoogleId);      
    }
  } 
}
