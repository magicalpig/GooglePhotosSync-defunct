using VideoToolsCore.Services.User;

namespace WebHelper.Services;
public class WebApiCurrentUserService(IHttpContextAccessor httpContextAccessor) : IGoogleCurrentUserService
{
    public string UserId { get; private set; } = httpContextAccessor.HttpContext?.Items["UserId"] as string ?? throw new ArgumentNullException("UserId", "Most likely cause is the request did not include the X-UserId header.");
}