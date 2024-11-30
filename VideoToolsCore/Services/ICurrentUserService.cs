namespace VideoToolsCore.Services;

public interface ICurrentUserService
{
    string UserId { get; }
}

public interface IGoogleCurrentUserService : ICurrentUserService { }
public interface ILocalCurrentUserService : ICurrentUserService { }