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

    public HomeController(ILogger<HomeController> logger, MongoDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Services()
    {
        var services = _context.Service?
            .Find(s => s.Status == "Published") // Fetch all services
            .ToList() ?? new List<Service>();

        return View(services);
    }

    [Route("Blogs")]
    public IActionResult Blogs()
    {
        var blogs = _context.Blog?
            .Find(b => b.Status == "Published")
            .Project<Blog>(Builders<Blog>.Projection
                .Include(b => b.Title)
                .Include(b => b.Author)
                .Include(b => b.Description)
                .Include(b => b.HeaderImageUrl)
                .Include(b => b.Slug)         // Needed for generating SEO-friendly links
                .Include(b => b.DatePublished) // For sorting and display
            )
            .SortByDescending(b => b.DatePublished)
            .ToList() ?? new List<Blog>();

        return View(blogs);
    }

    [Route("Blogs/{slug}")]
    public IActionResult BlogDetails(string slug)
    {
        var blog = _context.Blog?
            .Find(b => b.Slug == slug && b.Status == "Published")
            .FirstOrDefault();

        if (blog == null)
        {
            return NotFound(); // Returns 404 if the blog is not found
        }

        return View(blog);
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
