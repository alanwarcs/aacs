using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
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
        // Ensure session is available
        if (!context.Session.IsAvailable)
        {
            await _next(context);
            return;
        }

        // Get or create session ID
        string sessionId = context.Session.GetString("SessionId") ?? Guid.NewGuid().ToString();
        context.Session.SetString("SessionId", sessionId);

        var visitor = await _visitorsLogCollection
            .Find(v => v.SessionId == sessionId)
            .FirstOrDefaultAsync();

        bool isAdmin = context.User?.Identity?.IsAuthenticated == true; // Check if user is logged in

        if (visitor == null)
        {
            // Detect user details
            string ipAddress = GetCleanedIpAddress(context.Connection.RemoteIpAddress?.ToString() ?? "Unknown");
            var userAgent = context.Request.Headers["User-Agent"].ToString();
            var parsedUA = Parser.GetDefault().Parse(userAgent);

            var visitorLog = new VisitorsLog
            {
                SessionId = sessionId,
                VisitDate = DateTime.UtcNow,
                IpAddress = ipAddress,
                Browser = parsedUA.UA.Family,  // Detect browser
                OS = parsedUA.OS.Family,        // Detect OS
                Device = string.IsNullOrEmpty(parsedUA.Device.Family) ? "Desktop" : parsedUA.Device.Family, // Detect device
                Country = "Fetching...",
                City = "Fetching...",
                PagesVisited = new List<string> { context.Request.Path },
                UserType = isAdmin ? "Admin" : "Visitor"  // Set user type based on login status
            };

            // Fetch IP location data
            await FetchGeoLocation(visitorLog);

            await _visitorsLogCollection.InsertOneAsync(visitorLog);
        }
        else
        {
            // Update visited pages only if it's a new page
            if (!visitor.PagesVisited.Contains(context.Request.Path))
            {
                visitor.PagesVisited.Add(context.Request.Path);
            }

            // If user logs in, update UserType immediately
            if (isAdmin && visitor.UserType != "Admin")
            {
                visitor.UserType = "Admin";
            }

            await _visitorsLogCollection.ReplaceOneAsync(v => v.Id == visitor.Id, visitor);
        }

        await _next(context);
    }

    // ✅ Method to clean IP address (removes IPv6-mapped IPv4 addresses like ::ffff:100.64.0.2)
    private string GetCleanedIpAddress(string ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress)) return "Unknown";

        // If IPv6-mapped IPv4 (e.g., "::ffff:192.168.1.1"), extract actual IPv4
        if (ipAddress.StartsWith("::ffff:"))
        {
            ipAddress = ipAddress.Substring(7);
        }

        // If localhost or private IP, set to "Unknown" (prevents API errors)
        if (ipAddress == "127.0.0.1" || ipAddress == "::1")
        {
            return "Unknown";
        }

        return ipAddress;
    }

    // ✅ Fetch geolocation data using a free IP API
    private async Task FetchGeoLocation(VisitorsLog visitor)
    {
        try
        {
            if (visitor.IpAddress == "Unknown")
            {
                visitor.Country = "Unknown";
                visitor.City = "Unknown";
                return;
            }

            var response = await _httpClient.GetStringAsync($"https://ipinfo.io/{visitor.IpAddress}/json");
            var geoData = JsonSerializer.Deserialize<GeoData>(response);

            if (geoData != null)
            {
                visitor.Country = geoData.Country ?? "Unknown";
                visitor.City = geoData.City ?? "Unknown";
            }
        }
        catch
        {
            visitor.Country = "Unknown";
            visitor.City = "Unknown";
        }
    }
}

// ✅ Geolocation data model
public class GeoData
{
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}
