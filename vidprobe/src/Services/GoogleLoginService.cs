using Microsoft.Extensions.Logging;
using Google.Apis.Auth.OAuth2;
using System.Net.Http.Headers;

namespace VidProbe.Services;

// not using CasCap library to login (get auth tokens) with google because it required client secret to be sent.
// The http client is a singleton throughout entire app. It is also used by CasCap library.
// When login is successful, the access token is set on the shared http client as a header.
public class GoogleLoginService(ILogger<GoogleLoginService> logger, VidProbeSettings settings, HttpClient sharedClient)
{
  // injected dependencies
  private readonly ILogger<GoogleLoginService> _logger = logger;
  private readonly VidProbeSettings _settings = settings;
  private readonly HttpClient _sharedClient = sharedClient;

  private bool _isLoggedIn = false;

  public async Task<bool> LoginAsync()
  {    
    if (_isLoggedIn)
    {
      _logger.LogDebug("Already logged in, skipping login");
      return true;
    }
    string localUserId = _settings.LocalUserId ?? "userxyz";
    _logger.LogDebug("Sending {localUserId} to Google to get authorization tokens", localUserId);
    UserCredential credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
      new ClientSecrets { ClientId = _settings.GoogleAPIClientId },
      [ "https://www.googleapis.com/auth/photoslibrary.readonly" ],
      localUserId,
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
    _isLoggedIn = true;
    return true;
  }

  public async Task<string> GetUserIdAsync()
  {
    if (!_isLoggedIn)
      await LoginAsync();
    return "poopoo";
  }
}