using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using aacs.Models;
using MongoDB.Driver;

public class ContactController : Controller
{
    private readonly MongoDbContext _context;

    // Constructor to inject MongoDbContext
    public ContactController(MongoDbContext context)
    {
        _context = context;
    }

    [Authorize]
    public IActionResult ContactsManagement(int page = 1)
    {
        const int pageSize = 10;
        var contacts = _context.Contact.Find(_ => true)
                                       .SortBy(c => c.Id)
                                       .Skip((page - 1) * pageSize)
                                       .Limit(pageSize)
                                       .ToList();

        var totalContacts = _context.Contact.CountDocuments(_ => true);
        var totalPages = (int)Math.Ceiling(totalContacts / (double)pageSize);

        var model = new PaginatedList<Contact>(contacts, (int)totalContacts, page, pageSize);

        ViewData["TotalPages"] = totalPages;
        ViewData["CurrentPage"] = page;

        return View("~/Views/Admin/ContactsManagement.cshtml", model);
    }

    [HttpPost("api/contact")]
    [AllowAnonymous] // ⛔ Important: Allow anonymous access
    public IActionResult CreateContact([FromBody] Contact contact)
    {
        if (ModelState.IsValid)
        {
            try
            {
                _context.Contact.InsertOne(contact);
                return Ok(new { success = true, message = "Message sent successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Database error: " + ex.Message });
            }
        }

        var errors = ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .ToList();

        return BadRequest(new { success = false, errors });
    }

    [HttpPost]
    [Authorize]
    public IActionResult DeleteContact(string id)
    {
        try
        {
            var contact = _context.Contact.Find(c => c.Id == new MongoDB.Bson.ObjectId(id)).FirstOrDefault();
            if (contact == null)
            {
                TempData["ErrorMessage"] = "Contact not found!";
                return RedirectToAction("ContactsManagement");
            }

            _context.Contact.DeleteOne(c => c.Id == new MongoDB.Bson.ObjectId(id));
            TempData["SuccessMessage"] = "Contact deleted successfully!";
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "There was an error deleting the Contact. Please try again.";
        }

        return RedirectToAction("ContactsManagement");
    }

    [HttpPost]
    [Authorize]
    public IActionResult MarkAsRead(string id)
    {
        var filter = Builders<Contact>.Filter.Eq(c => c.Id, new MongoDB.Bson.ObjectId(id));
        var update = Builders<Contact>.Update.Set(c => c.IsRead, true);
        _context.Contact.UpdateOne(filter, update);
        return Json(new { success = true });
    }
}