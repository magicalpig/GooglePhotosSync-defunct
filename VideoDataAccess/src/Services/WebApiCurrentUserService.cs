namespace VideoDataAccess.Services;
public class WebApiCurrentUserService : ICurrentUserService
{
  public string UserId { get; private set; }
  public WebApiCurrentUserService() 
  {
    UserId = "user2@example.com";
  }  
}