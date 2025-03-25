using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

public class BotDetectionMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly ConcurrentDictionary<string, (DateTime lastRequest, int requestCount)> RequestTracker = new();

    public BotDetectionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var userAgent = context.Request.Headers["User-Agent"].FirstOrDefault() ?? string.Empty;
        var requestHeaders = context.Request.Headers;
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "UnknownIP";

        if (IsSuspiciousRequest(userAgent, requestHeaders, ip, context))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Access denied.");
            return;
        }

        await _next(context);
    }

    private bool IsSuspiciousRequest(string userAgent, IHeaderDictionary headers, string ip, HttpContext context)
    {
        // Heuristic 1: Block missing or short User-Agent
        if (string.IsNullOrEmpty(userAgent) || userAgent.Length < 10)
        {
            return true;
        }

        // Heuristic 2: Block known bot indicators in User-Agent
        var botIndicators = new[] { "bot", "crawler", "spider", "scraper" };
        if (botIndicators.Any(indicator => userAgent.ToLower().Contains(indicator)))
        {
            return true;
        }

        // Heuristic 3: Block requests with bot headers
        if (headers.ContainsKey("X-Bot-Flag") || headers.ContainsKey("X-Scraper"))
        {
            return true;
        }

        // Heuristic 4: Block high-frequency requests (Rate Limiting)
        if (!string.IsNullOrEmpty(ip))
        {
            var now = DateTime.UtcNow;
            if (RequestTracker.TryGetValue(ip, out var record))
            {
                if ((now - record.lastRequest).TotalSeconds < 1) // More than 1 request per second
                {
                    record.requestCount++;
                    if (record.requestCount > 10) // More than 10 requests in 10 seconds
                    {
                        return true; // Block bot
                    }
                }
                else
                {
                    record.requestCount = 1;
                }

                RequestTracker[ip] = (now, record.requestCount);
            }
            else
            {
                RequestTracker[ip] = (now, 1);
            }
        }

        // Heuristic 5: Verify human token (make this optional or configurable)
        bool requireHumanToken = false; // Set to true if you want to enforce this check
        if (requireHumanToken && (!context.Request.Headers.TryGetValue("X-Human-Token", out var token) || token != "human-verification-token"))
        {
            return true;
        }

        return false;
    }
}