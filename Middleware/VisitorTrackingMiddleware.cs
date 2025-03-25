using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using aacs.Models;
using UAParser;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using System.Linq;

public class VisitorTrackingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMongoCollection<VisitorsLog> _visitorCollection;
    private readonly ILogger<VisitorTrackingMiddleware> _logger;
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;

    public VisitorTrackingMiddleware(RequestDelegate next, IMongoCollection<VisitorsLog> visitorCollection, ILogger<VisitorTrackingMiddleware> logger, IHttpClientFactory httpClientFactory, IMemoryCache cache)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _visitorCollection = visitorCollection ?? throw new ArgumentNullException(nameof(visitorCollection));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClientFactory?.CreateClient() ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            // Ensure session is available
            if (!context.Session.IsAvailable)
                await context.Session.LoadAsync();

            // Generate or retrieve session ID
            string sessionId = context.Session.GetString("VisitorSessionId") ?? Guid.NewGuid().ToString();
            context.Session.SetString("VisitorSessionId", sessionId);

            // Extract User-Agent (handle missing values)
            string userAgent = context.Request.Headers["User-Agent"].FirstOrDefault() ?? "Unknown";
            var uaParser = Parser.GetDefault();
            var clientInfo = uaParser.Parse(userAgent);
            string browser = clientInfo?.UA?.Family ?? "Unknown";
            string device = clientInfo?.Device?.Family ?? "Unknown";

            // Improved browser detection (for Brave, Edge, and Opera)
            if (userAgent.Contains("Brave", StringComparison.OrdinalIgnoreCase)) browser = "Brave";
            else if (userAgent.Contains("Edg/", StringComparison.OrdinalIgnoreCase)) browser = "Edge";
            else if (userAgent.Contains("OPR", StringComparison.OrdinalIgnoreCase) || userAgent.Contains("Opera")) browser = "Opera";

            // Get the page visited
            string pageVisited = context.Request.Path;

            // Retrieve the real IP Address (considering proxy headers)
            string ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                               ?? context.Connection.RemoteIpAddress?.ToString()
                               ?? "Unknown";

            // Fetch country using IP geolocation
            string country = await GetCountryFromIP(ipAddress);

            // Find existing log by session ID
            var filter = Builders<VisitorsLog>.Filter.Eq(x => x.SessionId, sessionId);
            var existingLog = await _visitorCollection.Find(filter).FirstOrDefaultAsync();

            if (existingLog != null)
            {
                if (!existingLog.PagesVisited.Contains(pageVisited))
                {
                    var update = Builders<VisitorsLog>.Update.Push(x => x.PagesVisited, pageVisited);
                    await _visitorCollection.UpdateOneAsync(filter, update);
                }
            }
            else
            {
                var visitorLog = new VisitorsLog
                {
                    SessionId = sessionId,
                    VisitDate = DateTime.UtcNow,
                    Browser = browser,
                    Device = device,
                    Country = country,
                    PagesVisited = new List<string> { pageVisited },
                    IpAddress = ipAddress
                };

                await _visitorCollection.InsertOneAsync(visitorLog);
            }

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error saving visitor log: {ex.Message}");
            await _next(context);
        }
    }

    private async Task<string> GetCountryFromIP(string ipAddress)
    {
        if (_cache.TryGetValue(ipAddress, out string? cachedCountry) && !string.IsNullOrEmpty(cachedCountry))
            return cachedCountry;

        try
        {
            var response = await _httpClient.GetAsync($"http://ip-api.com/json/{ipAddress}");
            if (response != null && response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("country", out var countryElement))
                {
                    string country = countryElement.GetString() ?? "Unknown";
                    _cache.Set(ipAddress, country, TimeSpan.FromHours(24)); // Cache the result
                    return country;
                }
            }
            else
            {
                _logger.LogWarning($"IP-API request failed for IP {ipAddress}. Status code: {response?.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching geolocation for IP {ipAddress}: {ex.Message}");
        }

        return "Unknown";
    }
}