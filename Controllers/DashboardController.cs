using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using aacs.Models; // Assuming your models are in the "aacs.Models" namespace
using MongoDB.Driver; // Add this line to import the MongoDB driver
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace aacs.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IMongoCollection<Blog> _blogCollection;
        private readonly IMongoCollection<Contact> _contactCollection;
        private readonly IMongoCollection<Service> _servicesCollection;
        private readonly IMongoCollection<VisitorsLog> _visitorCollection;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(IMongoDatabase database, ILogger<DashboardController> logger)
        {
            _blogCollection = database.GetCollection<Blog>("Blog");
            _contactCollection = database.GetCollection<Contact>("Contact");
            _servicesCollection = database.GetCollection<Service>("Service");
            _visitorCollection = database.GetCollection<VisitorsLog>("VisitorLog");
            _logger = logger;
        }

        public async Task<IActionResult> Dashboard()
        {
            try
            {
                // Fetch and log actual data
                var blogs = await _blogCollection.Find(FilterDefinition<Blog>.Empty).ToListAsync();
                var contacts = await _contactCollection.Find(FilterDefinition<Contact>.Empty).ToListAsync();
                var services = await _servicesCollection.Find(FilterDefinition<Service>.Empty).ToListAsync();
                var totalVisitors = await _visitorCollection.CountDocumentsAsync(FilterDefinition<VisitorsLog>.Empty);

                // Log data
                _logger.LogInformation($"Retrieved Blogs: {blogs.Count}");
                foreach (var blog in blogs)
                {
                    _logger.LogInformation($"Blog Title: {blog.Title}");
                }

                _logger.LogInformation($"Retrieved Contacts: {contacts.Count}");
                _logger.LogInformation($"Retrieved Services: {services.Count}");
                foreach (var service in services)
                {
                    _logger.LogInformation($"Service Name: {service.Title}");
                }

                _logger.LogInformation($"Total Visitors: {totalVisitors}");

                // Store the count in ViewBag
                ViewBag.TotalBlogs = blogs.Count;
                ViewBag.NewContacts = contacts.Count;
                ViewBag.TotalServices = services.Count;
                ViewBag.TotalVisitors = totalVisitors;

                return View("~/Views/Admin/Dashboard.cshtml");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching dashboard data: {ex.Message}");
                ViewBag.ErrorMessage = "Error fetching dashboard data. Please check the logs.";
                return View("~/Views/Admin/Dashboard.cshtml");
            }
        }
    }
}
