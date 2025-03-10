using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

[Route("sitemap.xml")]
[ApiController]
public class SitemapController : ControllerBase
{
    [HttpGet]
    public IActionResult GenerateSitemap()
    {
        var urls = new List<string>
        {
            "https://alanwar.studio/",
            "https://alanwar.studio/Services",
            "https://alanwar.studio/Blogs",
            "https://alanwar.studio/Contact",
            "https://alanwar.studio/About",   
        };

        var sitemap = new XDocument(
            new XDeclaration("1.0", "utf-8", "yes"),
            new XElement("urlset",
                new XAttribute("xmlns", "http://www.sitemaps.org/schemas/sitemap/0.9"),
                urls.Select(url => new XElement("url",
                    new XElement("loc", url),
                    new XElement("lastmod", DateTime.UtcNow.ToString("yyyy-MM-dd")),
                    new XElement("changefreq", "daily"),
                    new XElement("priority", "0.8")
                ))
            )
        );

        return Content(sitemap.ToString(), "application/xml", Encoding.UTF8);
    }
}
