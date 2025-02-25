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
            .SortByDescending(b => b.DatePublished)
            .ToList() ?? new List<Blog>();

        return View(blogs);
    }


    [Route("Blogs/{id}")]
    public IActionResult BlogDetails(string id)
    {
        var blog = _context.Blog?
            .Find(b => b.Id == new MongoDB.Bson.ObjectId(id) && b.Status == "Published")
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

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
