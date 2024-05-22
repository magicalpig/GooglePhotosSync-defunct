namespace VideoDataAccess.Services;
public class LocalComputerCurrentUserService : ICurrentUserService
{
  public string UserId { get; private set; }
  public LocalComputerCurrentUserService() 
  {
    UserId = Environment.UserName;
  }  
}