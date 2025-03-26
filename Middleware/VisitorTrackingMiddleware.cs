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

        // Get real IP
        string ipAddress = await GetRealIpAddress(context);

        if (visitor == null)
        {
            // Detect user details
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

    // ✅ Fetch the real IP address (handles proxies, CGNAT, etc.)
    private async Task<string> GetRealIpAddress(HttpContext context)
    {
        string ipAddress = "Unknown";

        // 1️⃣ Check for forwarded IP (if using a proxy)
        if (context.Request.Headers.ContainsKey("X-Forwarded-For"))
        {
            ipAddress = context.Request.Headers["X-Forwarded-For"].ToString().Split(',')[0].Trim();
        }
        else
        {
            ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        // 2️⃣ Clean IPv6-mapped IPv4 addresses (e.g., "::ffff:192.168.1.1" → "192.168.1.1")
        if (ipAddress.StartsWith("::ffff:"))
        {
            ipAddress = ipAddress.Substring(7);
        }

        // 3️⃣ If private/CGNAT IP, fetch public IP from ipify
        if (IsPrivateIp(ipAddress))
        {
            try
            {
                var publicIpResponse = await _httpClient.GetStringAsync("https://api64.ipify.org?format=json");
                var publicIpData = JsonSerializer.Deserialize<PublicIpData>(publicIpResponse);
                if (publicIpData != null && !string.IsNullOrEmpty(publicIpData.Ip))
                {
                    ipAddress = publicIpData.Ip;
                }
            }
            catch
            {
                ipAddress = "Unknown";
            }
        }

        return ipAddress;
    }

    // ✅ Check if IP is private (Localhost, CGNAT, etc.)
    private bool IsPrivateIp(string ip)
    {
        return ip.StartsWith("10.") || ip.StartsWith("192.168.") || ip.StartsWith("172.16.") || ip.StartsWith("100.64.") ||
               ip == "127.0.0.1" || ip == "::1" || ip == "Unknown";
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
                visitor.Country = geoData.country ?? "Unknown";
                visitor.City = geoData.city ?? "Unknown";
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
    public string city { get; set; } = string.Empty;
    public string country { get; set; } = string.Empty;
}

// ✅ Public IP API response model
public class PublicIpData
{
    public string Ip { get; set; } = string.Empty;
}
