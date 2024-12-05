using VideoToolsCore.Utilities;

namespace VideoToolsCore.User.Services;

public class SettingsGoogleCurrentUserService(BaseSettings settings) : IGoogleCurrentUserService
{    
    public string UserId { get; private set; } = settings.GetRequiredSetting<string>("GoogleOwner");
}