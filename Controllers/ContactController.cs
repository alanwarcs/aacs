using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using aacs.Models;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;

public class ContactController : Controller
{

    private readonly ApplicationDbContext _context;

    // Constructor to inject ApplicationDbContext
    public ContactController(ApplicationDbContext context)
    {
        _context = context;
    }

    [Authorize]
    public IActionResult ContactsManagement()
    {
        const int pageSize = 2;
        int page = 1; // Define the page variable
        var contacts = _context.Contact?.OrderBy(b => b.ContactId) // Sort by ID or another field if needed
                                .Skip((page - 1) * pageSize)
                                .Take(pageSize)
                                .ToList() ?? new List<Contact>(); // Fallback to an empty list if null

        var totalContacts = _context.Contact?.Count() ?? 0;
        var totalPages = (int)Math.Ceiling(totalContacts / (double)pageSize);

        var model = new PaginatedList<Contact>(contacts, totalContacts, page, pageSize);

        ViewData["TotalPages"] = totalPages; // Pass total pages to the view
        ViewData["CurrentPage"] = page; // Pass current page to the view


        return View("~/Views/Admin/ContactsManagement.cshtml");
    }

    [HttpPost]
    public IActionResult CreateContact(Contact contact)
    {
        if (ModelState.IsValid)
        {
            try
            {
                _context?.Contact?.Add(contact);
                _context?.SaveChanges();
                TempData["SuccessMessage"] = "Contact created successfully!";

                // Redirect to the original location
                var referer = Request.Headers["Referer"].ToString();
                if (!string.IsNullOrEmpty(referer))
                {
                    return Redirect(referer);
                }
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Database error: " + ex.Message;
            }
        }
        else
        {
            var errorMessages = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

            TempData["ValidationErrors"] = errorMessages;
        }

        // If we reach here, it means there was a validation error or an exception
        var refererUrl = Request.Headers["Referer"].ToString();
        if (refererUrl.Contains("Contact"))
        {
            return View("~/Views/Home/Contact.cshtml", contact);
        }
        return View("~/Views/Home/Index.cshtml", contact);
    }
}