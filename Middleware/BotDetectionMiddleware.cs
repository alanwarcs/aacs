using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

public class BotDetectionMiddleware
{
    private readonly RequestDelegate _next;

    public BotDetectionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var userAgent = context.Request.Headers["User-Agent"].ToString();
        if (string.IsNullOrEmpty(userAgent) || userAgent.Contains("bot", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.Redirect("/AccessDenied");
            return;
        }

        await _next(context);
    }
}
