using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UAParser;

public class VisitorTrackingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMongoCollection<VisitorsLog> _visitorsLogCollection;
    private readonly HttpClient _httpClient;

    public VisitorTrackingMiddleware(RequestDelegate next, IMongoCollection<VisitorsLog> visitorsLogCollection)
    {
        _next = next;
        _visitorsLogCollection = visitorsLogCollection;
        _httpClient = new HttpClient();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User?.Identity == null || !context.User.Identity.IsAuthenticated || !context.User.IsInRole("Admin"))
        {
            // Ensure session is enabled
            if (!context.Session.IsAvailable)
            {
                await _next(context);
                return;
            }

            // Get or Set Session ID (Prevents new visitor on every page load)
            string sessionId = context.Session.GetString("SessionId") ?? string.Empty;
            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = Guid.NewGuid().ToString();
                context.Session.SetString("SessionId", sessionId);
            }

            // Extract User-Agent
            var userAgent = context.Request.Headers["User-Agent"].ToString();
            var uaParser = Parser.GetDefault();
            var clientInfo = uaParser.Parse(userAgent);

            // Correct Browser Name (Detect Brave, Edge, Opera)
            string browserName = DetectBrowser(userAgent, clientInfo.UA.Family);

            // Extract IP Address
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            // Handle Localhost Cases
            string country = "Unknown", city = "Unknown";
            if (ipAddress == "::1" || ipAddress == "127.0.0.1")  
            {
                country = "Localhost";
                city = "N/A";
            }
            else
            {
                try
                {
                    var response = await _httpClient.GetStringAsync($"https://ipwho.is/{ipAddress}");
                    var geoInfo = JObject.Parse(response);
                    if (geoInfo["success"]?.ToObject<bool>() == true)
                    {
                        country = geoInfo["country"]?.ToString() ?? "Unknown";
                        city = geoInfo["city"]?.ToString() ?? "Unknown";
                    }
                }
                catch { /* Ignore errors */ }
            }

            // Check if visitor already exists in the database
            var existingVisitor = await _visitorsLogCollection
                .Find(v => v.SessionId == sessionId)
                .FirstOrDefaultAsync();

            if (existingVisitor != null)
            {
                // Update pages visited
                existingVisitor.PagesVisited.Add(context.Request.Path);
                await _visitorsLogCollection.ReplaceOneAsync(v => v.Id == existingVisitor.Id, existingVisitor);
            }
            else
            {
                // Create new visitor log entry
                var visitorLog = new VisitorsLog
                {
                    SessionId = sessionId,
                    VisitDate = DateTime.UtcNow,
                    IpAddress = ipAddress,
                    Browser = browserName,
                    Device = string.IsNullOrEmpty(clientInfo.Device.Family) ? "Unknown" : clientInfo.Device.Family,
                    OS = clientInfo.OS.Family,
                    Country = country,
                    City = city,
                    PagesVisited = new List<string> { context.Request.Path },
                    UserType = "Visitor"
                };

                // Insert into MongoDB
                await _visitorsLogCollection.InsertOneAsync(visitorLog);
            }
        }

        await _next(context);
    }

    private string DetectBrowser(string userAgent, string defaultBrowser)
    {
        if (userAgent.Contains("Brave", StringComparison.OrdinalIgnoreCase))
            return "Brave";
        if (userAgent.Contains("Edg/", StringComparison.OrdinalIgnoreCase))
            return "Edge";
        if (userAgent.Contains("OPR", StringComparison.OrdinalIgnoreCase) || userAgent.Contains("Opera", StringComparison.OrdinalIgnoreCase))
            return "Opera";

        return defaultBrowser;
    }
}