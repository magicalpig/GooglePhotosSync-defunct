using VideoToolsCore.DataAccess.Models;

namespace VideoToolsCore.User.Services;

public interface IDualCurrentUserService
{
    IGoogleCurrentUserService? GoogleCurrentUserService { get; }
    ILocalCurrentUserService? LocalCurrentUserService { get; }
    public string? GetCurrentUserForEntity(OwnableEntityBase entity);
}