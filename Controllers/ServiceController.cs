using System.Linq;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using aacs.Models;

namespace aacs.Controllers;

public class ServiceController : Controller
{
    private readonly ApplicationDbContext _context;

    // Constructor to inject ApplicationDbContext
    public ServiceController(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    [Authorize]
    public IActionResult ServicesManagement(int page = 1)
    {
        const int pageSize = 2; // 2 rows per page
        var services = _context.Service?.OrderBy(s => s.ServiceId) // Sort by ID or another field if needed
                                .Skip((page - 1) * pageSize)
                                .Take(pageSize)
                                .ToList() ?? new List<Service>(); // Fallback to an empty list if null

        var totalServices = _context.Service?.Count() ?? 0;
        var totalPages = (int)Math.Ceiling(totalServices / (double)pageSize);

        var model = new PaginatedList<Service>(services, totalServices, page, pageSize);

        ViewData["TotalPages"] = totalPages; // Pass total pages to the view
        ViewData["CurrentPage"] = page; // Pass current page to the view

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

                _context.Service?.Add(service);
                _context.SaveChanges();

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
        return View("~/Views/Admin/ServicesManagement.cshtml", _context.Service?.ToList());
    }

    [HttpPost]
    [Authorize]
    public IActionResult DeleteService(int id)
    {
        try
        {
            var service = _context.Service?.FirstOrDefault(s => s.ServiceId == id);
            if (service == null)
            {
                TempData["ErrorMessage"] = "Service not found!";
                return RedirectToAction("ServicesManagement");
            }

            _context.Service?.Remove(service);
            _context.SaveChanges();
            TempData["SuccessMessage"] = "Service deleted successfully!";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error deleting admin: {ex.Message}";
        }

        return RedirectToAction("ServicesManagement");
    }

    [HttpGet]
    [Authorize]
    public IActionResult GetServiceDetails(int id){
        var service = _context.Service?.FirstOrDefault(s => s.ServiceId == id);
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
    public IActionResult UpdateService(Service updateService){
        
        if (ModelState.IsValid)
        {
            var service = _context.Service?.FirstOrDefault(s => s.ServiceId == updateService.ServiceId);
            if (service == null)
            {
                TempData["ErrorMessage"] = "Service not found.";
                return RedirectToAction("ServicesManagement");
            }
            // Update service fields
            service.Title = updateService.Title;
            service.Description = updateService.Description;
            service.Status = updateService.Status;

            if (service.Status == "Published")
            {
                service.DatePublished = DateTime.Now;
            }

            _context.SaveChanges();

            TempData["SuccessMessage"] = "service updated successfully!";
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
        return View("~/Views/Admin/ServicesManagement.cshtml", _context.Service?.ToList());
    }
}