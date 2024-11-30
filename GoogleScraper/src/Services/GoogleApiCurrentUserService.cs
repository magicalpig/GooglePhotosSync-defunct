using VideoToolsCore.Services.User;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;

namespace GoogleScraper.Services;

public class GoogleApiCurrentUserService : IGoogleCurrentUserService
{
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;
    private const string UserInfoUrl = "https://www.googleapis.com/oauth2/v2/userinfo";

    private readonly Lazy<Task<string>> _userId;

    public GoogleApiCurrentUserService(HttpClient httpClient, ILogger<GoogleApiCurrentUserService> logger)
    {
        _logger = logger;
        _httpClient = httpClient;
        _userId = new Lazy<Task<string>>(GetUserIdAsync);
    }

    public string UserId
    {
        get => _userId.Value.Result;
    }

    private async Task<string> GetUserIdAsync()
    {
        string? email = null;
        string? extraErrorInfo = null;

        var response = await _httpClient.GetAsync(UserInfoUrl);
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            try
            {
                var userInfo = JsonConvert.DeserializeObject<UserInfoResponse>(content);
                email = userInfo.Email;
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

        if (string.IsNullOrEmpty(email)) throw new InvalidOperationException(extraErrorInfo);
        else return email;
    }

    private struct UserInfoResponse
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public bool VerifiedEmail { get; set; }
        public string Picture { get; set; }
    }
}