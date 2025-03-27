using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using aacs.Models;

namespace aacs.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IMongoCollection<Blog> _blogCollection;
        private readonly IMongoCollection<Contact> _contactCollection;
        private readonly IMongoCollection<Service> _servicesCollection;
        private readonly IMongoCollection<VisitorsLog> _visitorLogCollection;

        public DashboardController(IMongoDatabase database)
        {
            _blogCollection = database.GetCollection<Blog>("Blog");
            _contactCollection = database.GetCollection<Contact>("Contact");
            _servicesCollection = database.GetCollection<Service>("Service");
            _visitorLogCollection = database.GetCollection<VisitorsLog>("VisitorsLogs");
        }

        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var blogs = await _blogCollection.CountDocumentsAsync(FilterDefinition<Blog>.Empty);
                var contacts = await _contactCollection.CountDocumentsAsync(FilterDefinition<Contact>.Empty);
                var services = await _servicesCollection.CountDocumentsAsync(FilterDefinition<Service>.Empty);
                var visitors = await _visitorLogCollection.CountDocumentsAsync(FilterDefinition<VisitorsLog>.Empty);

                ViewBag.TotalBlogs = blogs;
                ViewBag.NewContacts = contacts;
                ViewBag.TotalServices = services;
                ViewBag.TotalVisitors = visitors;

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
