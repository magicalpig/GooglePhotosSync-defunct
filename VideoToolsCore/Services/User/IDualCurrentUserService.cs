using VideoToolsCore.Models;

namespace VideoToolsCore.Services.User;

public interface IDualCurrentUserService
{
    IGoogleCurrentUserService? GoogleCurrentUserService { get; }
    ILocalCurrentUserService? LocalCurrentUserService { get; }
    public string? GetCurrentUserForEntity(OwnableEntityBase entity);
}