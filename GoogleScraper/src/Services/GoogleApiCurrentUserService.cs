using VideoDataAccess.Services;

namespace GoogleScraper.Services;
public class GoogleApiCurrentUserService(GoogleLoginService googleLoginService) : ICurrentUserService
{
  private readonly Lazy<Task<string>> _userId = new(googleLoginService.GetUserIdAsync);

  public string UserId 
  { 
    get => _userId.Value.Result;
  }
}