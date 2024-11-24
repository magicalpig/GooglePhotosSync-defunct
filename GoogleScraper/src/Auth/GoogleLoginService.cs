using Microsoft.Extensions.Logging;
using Google.Apis.Auth.OAuth2;
using System.Net.Http.Headers;


namespace GoogleScraper.Auth;

// not using CasCap library to login (get auth tokens) with google because it required client secret to be sent.
// Instead, this class will get the tokens and then persist them in a member property.
// The Http Client used by the rest of the app has a DelegatingHandler that will set the Authorization header
// on every request by calling GetAuthHeader.
public class GoogleLoginService(ILogger<GoogleLoginService> logger, Settings settings)
{
    // injected dependencies
    private readonly ILogger<GoogleLoginService> _logger = logger;
    private readonly Settings _settings = settings;

    private bool _isLoggedIn = false;
    private AuthenticationHeaderValue? _authHeader;


    public async Task<AuthenticationHeaderValue?> GetAuthHeader()
    {
        if (_isLoggedIn)
        {
            _logger.LogDebug("Already logged in, skipping login");
            return _authHeader;
        }
        string localUserId = _settings.LocalUserId ?? "userxyz";
        _logger.LogDebug("Sending {localUserId} to Google to get authorization tokens", localUserId);
        UserCredential credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
          new ClientSecrets { ClientId = _settings.GoogleAPIClientId },
          ["https://www.googleapis.com/auth/photoslibrary.readonly", "https://www.googleapis.com/auth/userinfo.email"],
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
                return null;
            }
        }
        else
        {
            _logger.LogDebug("The access token is OK, continue");
        }

        _authHeader = new AuthenticationHeaderValue(credential.Token.TokenType, credential.Token.AccessToken);
        _isLoggedIn = true;
        return _authHeader;
    }
}