using Microsoft.Extensions.Logging;

namespace GoogleScraper.Auth;

public class AuthorizationHandler(GoogleLoginService googleLoginService, ILogger<AuthorizationHandler> logger) : DelegatingHandler
{
    private readonly GoogleLoginService _loginService = googleLoginService;
    private readonly ILogger<AuthorizationHandler> _logger = logger;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Adding Authorization header to request");
        request.Headers.Authorization = await _loginService.GetAuthHeader();
        return await base.SendAsync(request, cancellationToken);
    }
}