using VideoDataAccess.Services;

namespace VidProbe.Services;
public class GoogleApiCurrentUserService : ICurrentUserService
{
  private readonly GoogleLoginService _googleLoginService;
  private readonly Lazy<Task<string>> _userId;
  
  public GoogleApiCurrentUserService(GoogleLoginService googleLoginService)
  {
    _googleLoginService = googleLoginService;
    _userId = new Lazy<Task<string>>(_googleLoginService.GetUserIdAsync);    
  }
  public string UserId 
  { 
    get => _userId.Value.Result;
  }
}