using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using aacs.Models;
using MongoDB.Driver;

namespace aacs.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly MongoDbContext _context;
    private readonly string _frontendUrl;

    public HomeController(ILogger<HomeController> logger, MongoDbContext context, IConfiguration configuration)
    {
        _logger = logger;
        _context = context;
        _frontendUrl = configuration["FrontendBaseUrl"]!;
    }


    public IActionResult Index()
    {
        return Redirect(_frontendUrl);
    }

    [HttpGet("api/services")]
    public IActionResult GetServicesJson()
    {
        var services = _context.Service?
            .Find(s => s.Status == "Published")
            .ToList() ?? new List<Service>();

        return Ok(services); // ✅ returns 200 OK with JSON
    }

    [HttpGet("api/blogs")]
    public IActionResult GetBlogsJson()
    {
        var blogs = _context.Blog?
            .Find(b => b.Status == "Published")
            .SortByDescending(b => b.DatePublished)
            .ToList() ?? new List<Blog>();

        return Ok(blogs);
    }

    [HttpGet("api/blogs/{slug}")]
    public IActionResult GetBlogBySlug(string slug)
    {
        var blog = _context.Blog?
            .Find(b => b.Slug == slug && b.Status == "Published")
            .FirstOrDefault();

        if (blog == null) return NotFound();

        return Ok(blog);
    }

    public IActionResult Contact()
    {
        return View();
    }

    public IActionResult About()
    {
        return View();
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View("~/Views/Shared/AccessDenied.cshtml");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
