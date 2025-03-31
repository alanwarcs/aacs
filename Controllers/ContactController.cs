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

    [HttpPost]
    public IActionResult CreateContact(Contact contact)
    {
        if (ModelState.IsValid)
        {
            try
            {
                _context.Contact.InsertOne(contact);
                TempData["SuccessMessage"] = "Message sent successfully! Thank you for reaching out—we'll be in touch soon.";

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