using Microsoft.Extensions.Logging;
using Google.Apis.Auth.OAuth2;
using Newtonsoft.Json;
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
  private const string UserInfoUrl = "https://www.googleapis.com/oauth2/v2/userinfo";

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
      [ "https://www.googleapis.com/auth/photoslibrary.readonly", "https://www.googleapis.com/auth/userinfo.email" ],
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
    string? email = null;
    string? extraErrorInfo = null;
    if (!_isLoggedIn)
      await LoginAsync();
    var response = await _sharedClient.GetAsync(UserInfoUrl);
    if (response.IsSuccessStatusCode)
    {
      var content = await response.Content.ReadAsStringAsync();
      try 
      {
        var userInfo = JsonConvert.DeserializeObject<UserInfoResponse>(content);
        email = userInfo?.Email;
      }
      catch (Exception ex)
      {
        extraErrorInfo = $"Failed to deserialize response from Google into a UserInfoResponse object. Response was {content}";
        _logger.LogError(ex, extraErrorInfo);
      }
    }
    else
    {
      extraErrorInfo = "Failed to get user info from Google. Maybe access token is old and doesn't include the https://www.googleapis.com/auth/userinfo.email scope? Try deleting the token from user's Roaming folder.";
      _logger.LogError("{msg}", extraErrorInfo);
    }

    if (email is null)      throw new InvalidOperationException(extraErrorInfo);
    else                    return email;      
  }

  private class UserInfoResponse
  {
    public string? Id { get; set; }
    public string? Email { get; set; }
    public bool? VerifiedEmail { get; set; }
    public string? Picture { get; set; }
  }

  public async Task<string> TestApiEndpoint(string url, string method = "GET")
  {
    if (!_isLoggedIn)
      await LoginAsync();
    var request = new HttpRequestMessage(new HttpMethod(method), url);
    var response = await _sharedClient.SendAsync(request);
    return await response.Content.ReadAsStringAsync();
  }
  
}