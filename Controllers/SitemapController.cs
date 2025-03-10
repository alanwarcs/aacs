using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

[Route("sitemap.xml")]
[ApiController]
public class SitemapController : ControllerBase
{
    private readonly ILogger<SitemapController> _logger;

    // ✅ Keep only this constructor (remove parameterless constructor)
    public SitemapController(ILogger<SitemapController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IActionResult GenerateSitemap()
    {
        try
        {
            _logger.LogInformation("Generating Sitemap...");

            var urls = new List<string>
            {
                "https://alanwar.studio/",
                "https://alanwar.studio/Services",
                "https://alanwar.studio/Blogs",
                "https://alanwar.studio/Contact",
                "https://alanwar.studio/About",   
            };

            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9"; // Define namespace

            var sitemap = new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XElement(ns + "urlset",  // Apply namespace here
                    urls.Select(url => new XElement(ns + "url",
                        new XElement(ns + "loc", url),
                        new XElement(ns + "lastmod", DateTime.UtcNow.ToString("yyyy-MM-dd")),
                        new XElement(ns + "changefreq", "daily"),
                        new XElement(ns + "priority", "0.8")
                    ))
                )
            );

            _logger.LogInformation("Sitemap generated successfully.");
            return Content(sitemap.ToString(), "application/xml", Encoding.UTF8);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error generating sitemap: {Message}", ex.Message);
            return StatusCode(500, "Internal Server Error");
        }
    }
}