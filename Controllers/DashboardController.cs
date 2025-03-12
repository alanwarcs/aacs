using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using aacs.Models; // Assuming your models are in the "aacs.Models" namespace
using MongoDB.Driver; // Add this line to import the MongoDB driver

namespace aacs.Controllers
{
    public class DashboardController : Controller
    {
        private readonly MongoDbContext _context;

        public DashboardController(MongoDbContext context)
        {
            _context = context;
        }

        [Authorize]
        public async Task<IActionResult> Dashboard()
        {
            // Fetch the counts from MongoDB
            var totalBlogs = await _context.Blog.CountDocumentsAsync(FilterDefinition<Blog>.Empty);
            var newContacts = await _context.Contact.CountDocumentsAsync(FilterDefinition<Contact>.Empty);
            var totalServices = await _context.Service.CountDocumentsAsync(FilterDefinition<Service>.Empty);


            // Pass the data to the view using ViewBag
            ViewBag.TotalBlogs = totalBlogs;
            ViewBag.NewContacts = newContacts;
            ViewBag.TotalServices = totalServices;

            return View("~/Views/Admin/Dashboard.cshtml");
        }
    }
}
