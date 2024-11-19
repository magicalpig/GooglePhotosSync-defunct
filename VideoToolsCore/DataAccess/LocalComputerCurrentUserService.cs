namespace VideoToolsCore.DataAccess;

public class LocalComputerCurrentUserService : ICurrentUserService
{
    public string UserId { get; private set; }
    public LocalComputerCurrentUserService()
    {
        UserId = Environment.UserName;
    }
}