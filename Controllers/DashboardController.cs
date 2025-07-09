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
        private readonly IMongoCollection<AdminLog> _adminLogCollection;

        public DashboardController(IMongoDatabase database)
        {
            _blogCollection = database.GetCollection<Blog>("Blog");
            _contactCollection = database.GetCollection<Contact>("Contact");
            _servicesCollection = database.GetCollection<Service>("Service");
            _adminLogCollection = database.GetCollection<AdminLog>("AdminLog");
        }

        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var adminLogs = await _adminLogCollection.Find(FilterDefinition<AdminLog>.Empty)
                                                          .SortByDescending(x => x.Timestamp)
                                                          .Limit(10)
                                                          .ToListAsync();
                ViewBag.AdminLogs = adminLogs;

                var analytics = new AnalyticsService();
                var countryData = await analytics.GetUsersByCountryAsync();
                ViewBag.CountryData = countryData;

                return View("~/Views/Admin/Dashboard.cshtml");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Error fetching dashboard data: {ex.Message}";
                return View("~/Views/Admin/Dashboard.cshtml");
            }
        }
    }
}
