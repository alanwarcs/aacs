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

        public DashboardController(IMongoDatabase database)
        {
            _blogCollection = database.GetCollection<Blog>("Blog");
            _contactCollection = database.GetCollection<Contact>("Contact");
            _servicesCollection = database.GetCollection<Service>("Service");
            _visitorCollection = database.GetCollection<VisitorsLog>("VisitorsLog");
        }

        public async Task<IActionResult> Dashboard()
        {
            try
            {
                // Filter to count only userType "Visitor" entries (ignores Admin and bots)
                var filter = Builders<VisitorsLog>.Filter.Eq(v => v.UserType, "Visitor");
                var totalVisitors = await _visitorCollection.CountDocumentsAsync(filter);
                
                // Fetch and log actual data
                var blogs = await _blogCollection.Find(FilterDefinition<Blog>.Empty).ToListAsync();
                var contacts = await _contactCollection.Find(FilterDefinition<Contact>.Empty).ToListAsync();
                var services = await _servicesCollection.Find(FilterDefinition<Service>.Empty).ToListAsync();
                
                // Compute dynamic website traffic data for last 7 days using VisitorsLog
                var startDate = DateTime.UtcNow.Date.AddDays(-6);
                var endDate = DateTime.UtcNow.Date.AddDays(1);
                var dateFilter = Builders<VisitorsLog>.Filter.And(
                    Builders<VisitorsLog>.Filter.Eq(v => v.UserType, "Visitor"),
                    Builders<VisitorsLog>.Filter.Gte(v => v.VisitDate, startDate),
                    Builders<VisitorsLog>.Filter.Lt(v => v.VisitDate, endDate)
                );
                var recentVisitors = await _visitorCollection.Find(dateFilter).ToListAsync();
                var trafficData = new List<object> { new object[] { "Day", "Visitors" } };
                for (int i = 0; i < 7; i++)
                {
                    var date = startDate.AddDays(i);
                    var dayLabel = date.ToString("ddd");
                    var count = recentVisitors.Count(v => v.VisitDate.Date == date);
                    trafficData.Add(new object[] { dayLabel, count });
                }
                ViewBag.TrafficData = trafficData;
                
                // Group browsers used by visitors for donut chart, merging browsers beyond top 4 into "Others"
                var browserGroup = recentVisitors.GroupBy(v => v.Browser)
                                                 .Select(g => new { Browser = g.Key, Count = g.Count() })
                                                 .OrderByDescending(x => x.Count)
                                                 .ToList();
                var donutData = new List<object> { new object[] { "Browser", "Count" } };
                var top4 = browserGroup.Take(4).ToList();
                foreach (var group in top4)
                {
                    donutData.Add(new object[] { group.Browser, group.Count });
                }
                var othersCount = browserGroup.Skip(4).Sum(x => x.Count);
                if (othersCount > 0)
                {
                    donutData.Add(new object[] { "Others", othersCount });
                }
                ViewBag.BrowserData = donutData;
                
                // Store counts in ViewBag
                ViewBag.TotalBlogs = blogs.Count;
                ViewBag.NewContacts = contacts.Count;
                ViewBag.TotalServices = services.Count;
                ViewBag.TotalVisitors = totalVisitors;
                
                return View("~/Views/Admin/Dashboard.cshtml");
            }
            catch (Exception)
            {
                ViewBag.ErrorMessage = "Error fetching dashboard data. Please check the logs.";
                return View("~/Views/Admin/Dashboard.cshtml");
            }
        }
    }
}
