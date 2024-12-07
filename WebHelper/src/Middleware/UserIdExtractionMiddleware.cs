namespace WebHelper.Middleware;

public class UserIdExtractionMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-UserId", out var userId))
        {
            context.Items["UserId"] = userId.ToString();
        }

        await _next(context);
    }

}