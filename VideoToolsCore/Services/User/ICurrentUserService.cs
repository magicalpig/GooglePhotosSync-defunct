namespace VideoToolsCore.Services.User;

public interface ICurrentUserService
{
    string UserId { get; }
}

public interface IGoogleCurrentUserService : ICurrentUserService { }
public interface ILocalCurrentUserService : ICurrentUserService { }