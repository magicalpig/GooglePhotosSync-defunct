namespace VideoToolsCore.Services;

public class LocalComputerCurrentUserService : ILocalCurrentUserService
{
    public string UserId { get; private set; }
    public LocalComputerCurrentUserService()
    {
        UserId = Environment.UserName;
    }
}