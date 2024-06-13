namespace GoogleScraper.Auth;

public class AuthorizationHandler(GoogleLoginService googleLoginService) : DelegatingHandler
{
  private readonly GoogleLoginService _loginService = googleLoginService;

  protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
  {
    request.Headers.Authorization = await _loginService.GetAuthHeader();
    return await base.SendAsync(request, cancellationToken);
  }
}