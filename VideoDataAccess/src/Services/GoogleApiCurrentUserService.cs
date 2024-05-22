namespace VideoDataAccess.Services;
public class GoogleApiCurrentUserService : ICurrentUserService
{
  public string UserId { get; private set; }
  public GoogleApiCurrentUserService() 
  {
    UserId = "user1@example.com";
  }  
}