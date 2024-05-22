namespace VideoDataAccess.Services;
public class WebBrowserCurrentUserService : ICurrentUserService
{
  public string UserId { get; private set; }
  public WebBrowserCurrentUserService() 
  {
    UserId = "user2@example.com";
  }  
}