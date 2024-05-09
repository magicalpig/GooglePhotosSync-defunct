// This gets a list of videos from Google Photos using the Google Photos REST API.
// It also queries a SQLite database to get video sizes and durations
// which were recorded in a previous run of the jsonToDb project.
// The only way to get size and duration is to use the batchexecute RPC API, 
// which I did directly in Chrome. I saved the results and fed them into jsonToDb so they could
// be saved in a SQLite database.
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Net;
using CasCap.Models;
using CasCap.Services;
using VidCommon;
using VidCommon.Models;
using Google.Apis.Auth.OAuth2;
using System.Net.Http.Headers;



namespace VidProbe;

public class GoogleConnector
{
  private const string _shortUrlBase = "https://photos.google.com/photo/";
  private static readonly Filter _videoOnlyFilter = new()
  {
    mediaTypeFilter = new() { mediaTypes = [GooglePhotosMediaType.VIDEO] }
  };
  
  private readonly HttpClient _sharedClient;
  private readonly GooglePhotosService _service;
  private readonly ILogger _logger;
  private readonly VideoContext _db;
  private readonly ConfigurationManager _config;

  public GoogleConnector(ConfigurationManager config, VideoContext dbIn)
  {
    _db = dbIn;
    _config = config;
    var loggingConfig = _config.GetSection("Logging");  
    using ILoggerFactory factory = LoggerFactory.Create(builder => 
    {      
      builder
        .AddConfiguration(loggingConfig)
        .AddConsole();
    });
    _logger = factory.CreateLogger<GoogleConnector>();          
    
    var apiSettings = _config.GetSection("GoogleAPI");
    var options = new GooglePhotosOptions() 
    {
      Scopes = [GooglePhotosScope.ReadOnly],
      User = "googiegoogie",
      ClientId = apiSettings["client_id"] ?? "",
      ClientSecret = "secretnotneededPKCE",
      // Token will be saved in C:\Users\Rance\AppData\Roaming\Google.Apis.Auth\
      // The next option can override this location
      // FileDataStoreFullPathOverride = @"./"
    };
    Console.WriteLine($"Created google photos options having client id {options.ClientId} and secret {options.ClientSecret}");
    
    var handler = new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate };
    _sharedClient = new HttpClient(handler) { BaseAddress = new Uri(options.BaseAddress) };
    _service = new GooglePhotosService(
        factory.CreateLogger<GooglePhotosService>(), 
        Options.Create(options),
        _sharedClient
    );
    
  }
  
  // not using CasCap's login method because I get an exception, probably having to do with the fact that
  // I'm trying to avoid sending the client secret in my token request.
  private async Task<bool> Login()
  {
    var apiSettings = _config.GetSection("GoogleAPI");
    string clientId = apiSettings?["client_id"] ?? "";
    UserCredential credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
      new ClientSecrets { ClientId = clientId },
      [ "https://www.googleapis.com/auth/photoslibrary.readonly" ],
      "googiegoogie",
      CancellationToken.None
    );

    _logger.LogInformation($"Access Token: {credential.Token.AccessToken}");
    _logger.LogInformation($"Refresh Token: {credential.Token.RefreshToken}");
    _logger.LogInformation($"Expiration Time: {credential.Token.ExpiresInSeconds} seconds");

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

  private async Task<bool> LoginOld()
  {        
    // On https://console.cloud.google.com/,
    //  app name is vidprobe.
    //  user1@example.com is registered as a test user -- only this account can be authenticated.
    //  credential is named "vidprobe client1" and it comes with a client id and a client secret.
    //  The client id and secret are stored in dotnet user-secrets.
    // Might launch OAuth consent screen in browser depending on whether refresh token is valid.
    if (!await _service.LoginAsync()) {
      _logger.LogError($"login failed!");
      return false;
    }
    else
      return true;
  }


  public async Task<List<VideoFileGoogle>> ScrapeVideos(int? pageLimit) {
    List<VideoFileGoogle> videos = [];
        
    var loginSuccess = await Login();
    IAsyncEnumerable<MediaItem> mediaItems =
      _service.GetMediaItemsByFilterAsync(_videoOnlyFilter, pageLimit ?? int.MaxValue);
    
    await foreach(var m in mediaItems) 
    {
      _logger.LogDebug("Processing Google media item {mediaItemId}", m.id);
      // pull metadata for this video obtained by jsonToDb project from the database
      VideoMetadataFromChrome? metadataFromChrome = _db.VideoMetadataFromChrome.FirstOrDefault(x => x.RestId == m.id);
      var v = new VideoFileGoogle(){
        GoogleId = m.id,
        Name = m.filename,
        CreatedDate = m.mediaMetadata.creationTime.ToLocalTime(),
        Description = m.description,
        Size = metadataFromChrome?.Filesize ?? 0,
        URL = metadataFromChrome?.ProductId is not null ? _shortUrlBase + metadataFromChrome.ProductId : null,
        DurationMs = metadataFromChrome?.DurationMs ?? 0
      };

      _logger.LogDebug("Adding to list: {googleId}", v.GoogleId);
      videos.Add(v);      
    }

    return videos;
  } 
}
