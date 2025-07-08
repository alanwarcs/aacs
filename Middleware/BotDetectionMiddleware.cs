using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using System.Threading.Tasks;

public class BotDetectionMiddleware
{
    private readonly RequestDelegate _next;

    public BotDetectionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    private bool IsBotUserAgent(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
        {
            return true; // Empty User-Agent is suspicious
        }

        // Check if User-Agent contains "bot"
        if (userAgent.Contains("bot", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        string[] botKeywords = new[]
        {
            "crawl", "spider", "slurp", "mediapartners", "wget", "curl", 
            "python-requests", "scrapy", "httpclient", "SiteLockSpider", "bingbot",
            "ahrefsbot", "semrushbot", "mj12bot", "yandexbot", "dotbot", "teoma",
            "baiduspider", "gigabot", "exabot", "sogou", "ia_archiver", "archive.org_bot",
            "rogerbot", "seznambot", "duckduckbot", "slurp", "facebookexternalhit",
            "twitterbot", "linkedinbot", "embedly", "quora link preview", "showyoubot",
            "outbrain", "pinterest", "slackbot", "vkShare", "W3C_Validator", "xenu link sleuth",
            "Google Page Speed Insights", "Google-Adwords", "Google-Adsense", "Googlebot",
            "Google-Site-Verification", "Google Web Preview", "Google Keyword Planner",
            "Google Search Console", "Google Publisher Plugin", "Google Favicon",
            "Google Structured Data Testing Tool", "Google Rich Snippets Testing Tool",
            "Google AdSense", "Google-Site-Verification", "Google-Search-Console",
            "Google-Structured-Data-Testing-Tool", "Google-Rich-Snippets-Testing-Tool",
            "Google-AdSense", "Google-AdWords", "Google-Keyword-Planner", "Google-Web-Preview",
            "Google-Page-Speed-Insights", "Googlebot-Image", "Googlebot-News", "Googlebot-Video",
            "Googlebot-Mobile", "Googlebot-Mobile", "Googlebot-Mobile", "Googlebot-Mobile",	"Googlebot-Image",
                "Go-http-client"
            };

        foreach (var keyword in botKeywords)
        {
            if (userAgent.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
