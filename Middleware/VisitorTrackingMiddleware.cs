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
            string ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var userAgent = context.Request.Headers["User-Agent"].ToString();
            var parsedUA = Parser.GetDefault().Parse(userAgent);

            var visitorLog = new VisitorsLog
            {
                SessionId = sessionId,
                VisitDate = DateTime.UtcNow,
                IpAddress = ipAddress,
                Browser = parsedUA.UA.Family,
                OS = parsedUA.OS.Family,
                Device = string.IsNullOrEmpty(parsedUA.Device.Family) ? "Desktop" : parsedUA.Device.Family,
                Country = "Fetching...",
                City = "Fetching...",
                PagesVisited = new List<string> { context.Request.Path },
                UserType = isAdmin ? "Admin" : "Visitor"  // ✅ Set user type based on login status
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

            // ✅ If user logs in, update UserType immediately
            if (isAdmin && visitor.UserType != "Admin")
            {
                visitor.UserType = "Admin";
                await _visitorsLogCollection.ReplaceOneAsync(v => v.Id == visitor.Id, visitor);
            }
            else
            {
                await _visitorsLogCollection.ReplaceOneAsync(v => v.Id == visitor.Id, visitor);
            }
        }

        await _next(context);
    }

    private async Task FetchGeoLocation(VisitorsLog visitor)
    {
        try
        {
            // Use a free IP geolocation API
            var response = await _httpClient.GetStringAsync($"https://ipapi.co/{visitor.IpAddress}/json/");
            var geoData = JsonSerializer.Deserialize<GeoData>(response);

            if (geoData != null)
            {
                visitor.Country = geoData.CountryName ?? "Unknown";
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

public class GeoData
{
    public string City { get; set; } = string.Empty;
    public string CountryName { get; set; } = string.Empty;
}