using VideoToolsCore.Models;

namespace VideoToolsCore.Services.User;

public class DualCurrentUserService(IGoogleCurrentUserService? googleCurrentUserService = null, ILocalCurrentUserService? windowsCurrentUserService = null) : IDualCurrentUserService
{
    public IGoogleCurrentUserService? GoogleCurrentUserService { get; } = googleCurrentUserService;
    public ILocalCurrentUserService? LocalCurrentUserService { get; } = windowsCurrentUserService;

    public string? GetCurrentUserForEntity(OwnableEntityBase entity)
    {
        return entity switch
        {
            VideoFileGoogle or GoogleAlbum or AlbumMembership => GoogleCurrentUserService?.UserId,
            VideoFileWindows => LocalCurrentUserService?.UserId,
            _ => null
        };
    }
}