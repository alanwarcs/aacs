using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using aacs.Models;
using MongoDB.Driver;

namespace aacs.Controllers;

public class ServiceController : Controller
{
    private readonly MongoDbContext _context;

    // Constructor to inject MongoDbContext
    public ServiceController(MongoDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    [Authorize]
    public IActionResult ServicesManagement(int page = 1)
    {
        const int pageSize = 10;
        var services = _context.Service?.Find(_ => true)
                                .SortBy(s => s.Id)
                                .Skip((page - 1) * pageSize)
                                .Limit(pageSize)
                                .ToList() ?? new List<Service>();

        var totalServices = _context.Service?.CountDocuments(_ => true) ?? 0;
        var totalPages = (int)Math.Ceiling(totalServices / (double)pageSize);

        var model = new PaginatedList<Service>(services, (int)totalServices, page, pageSize);

        ViewData["TotalPages"] = totalPages;
        ViewData["CurrentPage"] = page;

        return View("~/Views/Admin/ServicesManagement.cshtml", model);
    }

    [HttpPost]
    [Authorize]
    public IActionResult AddService(Service service)
    {
        if (ModelState.IsValid)
        {
            try
            {
                if (service.Status == "Published")
                {
                    service.DatePublished = DateTime.Now;
                }

                _context.Service?.InsertOne(service);

                TempData["SuccessMessage"] = "New service added successfully!";
                return RedirectToAction("ServicesManagement");
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error: {ex.Message}");
                TempData["ErrorMessage"] = "There was an error adding the service. Please try again.";
            }
        }
        else
        {
            // If model validation fails, add error message to TempData
            var errorMessages = ModelState.Values
                                    .SelectMany(v => v.Errors)
                                    .Select(e => e.ErrorMessage)
                                    .ToList();

            // Save these errors to TempData for displaying them in the view
            TempData["ValidationErrors"] = errorMessages;
        }

        // Return the view directly instead of redirecting
        return View("~/Views/Admin/ServicesManagement.cshtml", _context.Service?.Find(_ => true).ToList());
    }

    [HttpPost]
    [Authorize]
    public IActionResult DeleteService(string id)
    {
        try
        {
            var service = _context.Service?.Find(s => s.Id == new MongoDB.Bson.ObjectId(id)).FirstOrDefault();
            if (service == null)
            {
                TempData["ErrorMessage"] = "Service not found!";
                return RedirectToAction("ServicesManagement");
            }

            _context.Service?.DeleteOne(s => s.Id == new MongoDB.Bson.ObjectId(id));
            TempData["SuccessMessage"] = "Service deleted successfully!";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error deleting service: {ex.Message}";
        }

        return RedirectToAction("ServicesManagement");
    }

    [HttpGet]
    [Authorize]
    public IActionResult GetServiceDetails(string id)
    {
        var service = _context.Service?.Find(s => s.Id == new MongoDB.Bson.ObjectId(id)).FirstOrDefault();
        if (service == null)
        {
            return NotFound(new { message = "Service not found." });
        }

        return Json(new
        {
            title = service.Title,
            description = service.Description,
            status = service.Status
        });
    }

    [HttpPost]
    [Authorize]
    public IActionResult UpdateService(Service updateService)
    {
        if (ModelState.IsValid)
        {
            var service = _context.Service?.Find(s => s.Id == updateService.Id).FirstOrDefault();
            if (service == null)
            {
                TempData["ErrorMessage"] = "Service not found.";
                return RedirectToAction("ServicesManagement");
            }

            // Update service fields
            var update = Builders<Service>.Update
                .Set(s => s.Title, updateService.Title)
                .Set(s => s.Description, updateService.Description)
                .Set(s => s.Status, updateService.Status);

            if (updateService.Status == "Published")
            {
                update = update.Set(s => s.DatePublished, DateTime.Now);
            }

            _context.Service?.UpdateOne(s => s.Id == updateService.Id, update);

            TempData["SuccessMessage"] = "Service updated successfully!";
            return RedirectToAction("ServicesManagement");
        }
        else
        {
            // If model validation fails, add error message to TempData
            var errorMessages = ModelState.Values
                                    .SelectMany(v => v.Errors)
                                    .Select(e => e.ErrorMessage)
                                    .ToList();

            // Save these errors to TempData for displaying them in the view
            TempData["ValidationErrors"] = errorMessages;
        }

        // Return the view directly instead of redirecting
        return View("~/Views/Admin/ServicesManagement.cshtml", _context.Service?.Find(_ => true).ToList());
    }
}