using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using aacs.Models;

namespace aacs.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
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
        return View();
    }

    [Route("Blogs")]
    public IActionResult Blogs()
    {
        var blogs = _context.Blog?
            .Where(b => b.Status == "Published")
            .OrderByDescending(b => b.DatePublished)
            .ToList() ?? new List<Blog>();

        return View(blogs);
    }

    [Route("Blogs/{id}")]
    public IActionResult BlogDetails(int id)
    {
        var blog = _context.Blog?.FirstOrDefault(b => b.BlogId == id && b.Status == "Published");

        if (blog == null)
        {
            return NotFound();
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
