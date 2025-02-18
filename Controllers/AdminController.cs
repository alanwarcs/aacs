using System.Linq;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using aacs.Models;
using MongoDB.Driver;

namespace aacs.Controllers;

public class AdminController : Controller
{
    private readonly MongoDbContext _context;

    public AdminController(MongoDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Login(string error)
    {
        if (!string.IsNullOrEmpty(error))
        {
            ViewBag.Error = error;
        }
        else if (TempData["ErrorMessage"] != null)
        {
            ViewBag.Error = TempData["ErrorMessage"];
        }

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string email, string password)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ViewBag.Error = "Email and Password are required.";
            return View();
        }

        // Hash the password for comparison
        string hashedPassword = HashPassword(password);

        // Check the database for the admin user
        var admin = _context.Admins.Find(a => a.Email == email && a.PasswordHash == hashedPassword).FirstOrDefault();

        if (admin == null)
        {
            ViewBag.Error = "Invalid Email or Password.";
            return View();
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, admin.Username ?? "Unknown"),
            new Claim(ClaimTypes.Email, admin.Email ?? string.Empty),
            new Claim("AdminId", admin.Id.ToString())
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTime.UtcNow.AddHours(24)
            }
        );

        return RedirectToAction("Dashboard", "Dashboard");
    }

    [HttpGet]
    [Authorize]
    public IActionResult UsersManagement(int page = 1)
    {
        const int pageSize = 10;

        var admins = _context.Admins.Find(_ => true)
                                    .SortBy(a => a.Id)
                                    .Skip((page - 1) * pageSize)
                                    .Limit(pageSize)
                                    .ToList();

        var totalAdmins = _context.Admins.CountDocuments(_ => true);
        var totalPages = (int)Math.Ceiling(totalAdmins / (double)pageSize);

        var model = new PaginatedList<Admin>(admins, (int)totalAdmins, page, pageSize);

        ViewData["TotalPages"] = totalPages;
        ViewData["CurrentPage"] = page;

        return View("~/Views/Admin/UsersManagement.cshtml", model);
    }

    [HttpPost]
    [Authorize]
    public IActionResult AddAdmin(Admin admin)
    {
        if (ModelState.IsValid)
        {
            var existingAdmin = _context.Admins.Find(a => a.Email == admin.Email).FirstOrDefault();
            if (existingAdmin != null)
            {
                TempData["ErrorMessage"] = "This email is already in use. Please choose a different one.";
                ModelState.AddModelError("Email", "This email is already in use. Please choose a different one.");
            }

            if (!ModelState.IsValid)
            {
                return View("UsersManagement", _context.Admins.Find(_ => true).ToList());
            }

            try
            {
                if (!string.IsNullOrEmpty(admin.PasswordHash))
                {
                    admin.PasswordHash = HashPassword(admin.PasswordHash);
                }
                else
                {
                    TempData["ErrorMessage"] = "Password cannot be empty.";
                    return View("UsersManagement", _context.Admins.Find(_ => true).ToList());
                }

                _context.Admins.InsertOne(admin);

                TempData["SuccessMessage"] = "Admin added successfully!";
                return RedirectToAction("UsersManagement");
            }
            catch
            {
                TempData["ErrorMessage"] = "There was an error adding the admin. Please try again.";
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

        return View("UsersManagement", _context.Admins.Find(_ => true).ToList());
    }

    [HttpPost]
    [Authorize]
    public IActionResult DeleteAdmin(string id)
    {
        try
        {
            var loggedInAdminId = User.FindFirst("AdminId")?.Value;

            if (loggedInAdminId != null && loggedInAdminId == id)
            {
                TempData["ErrorMessage"] = "You cannot delete your own account!";
                return RedirectToAction("UsersManagement");
            }

            var admin = _context.Admins.Find(a => a.Id == new MongoDB.Bson.ObjectId(id)).FirstOrDefault();
            if (admin == null)
            {
                TempData["ErrorMessage"] = "Admin not found!";
                return RedirectToAction("UsersManagement");
            }

            _context.Admins.DeleteOne(a => a.Id == new MongoDB.Bson.ObjectId(id));
            TempData["SuccessMessage"] = "Admin deleted successfully!";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error deleting admin: {ex.Message}";
        }

        return RedirectToAction("UsersManagement");
    }

    [HttpGet]
    [Authorize]
    public IActionResult GetAdminDetails(string id)
    {
        var admin = _context.Admins.Find(a => a.Id == new MongoDB.Bson.ObjectId(id)).FirstOrDefault();
        if (admin == null)
        {
            return NotFound(new { message = "Admin not found." });
        }

        return Json(new
        {
            username = admin.Username,
            email = admin.Email,
            phone = admin.Phone,
            address = admin.Address,
            status = admin.Status
        });
    }

    [HttpPost]
    [Authorize]
    public IActionResult UpdateAdmin(Admin updatedAdmin)
    {
        if (updatedAdmin.Username == null)
        {
            TempData["ErrorMessage"] = "Username is required.";
            return RedirectToAction("UsersManagement");
        }

        if (updatedAdmin.Email == null)
        {
            TempData["ErrorMessage"] = "Email is required.";
            return RedirectToAction("UsersManagement");
        }

        var loggedInAdminId = User.FindFirst("AdminId")?.Value;

        if (loggedInAdminId != null && loggedInAdminId == updatedAdmin.Id.ToString())
        {
            TempData["ErrorMessage"] = "You cannot edit your own account!";
            return RedirectToAction("UsersManagement");
        }

        var admin = _context.Admins.Find(a => a.Id == updatedAdmin.Id).FirstOrDefault();
        if (admin == null)
        {
            TempData["ErrorMessage"] = "Admin not found.";
            return RedirectToAction("UsersManagement");
        }

        var update = Builders<Admin>.Update
            .Set(a => a.Username, updatedAdmin.Username)
            .Set(a => a.Email, updatedAdmin.Email)
            .Set(a => a.Phone, updatedAdmin.Phone)
            .Set(a => a.Address, updatedAdmin.Address)
            .Set(a => a.Status, updatedAdmin.Status);

        if (!string.IsNullOrEmpty(updatedAdmin.PasswordHash))
        {
            update = update.Set(a => a.PasswordHash, HashPassword(updatedAdmin.PasswordHash));
        }

        _context.Admins.UpdateOne(a => a.Id == updatedAdmin.Id, update);
        TempData["SuccessMessage"] = "Admin updated successfully!";
        return RedirectToAction("UsersManagement");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["ErrorMessage"] = "You have been logged out.";
        return RedirectToAction("Login");
    }

    private string HashPassword(string password)
    {
        using (var sha256 = SHA256.Create())
        {
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            StringBuilder builder = new StringBuilder();
            foreach (var b in bytes)
            {
                builder.Append(b.ToString("x2"));
            }
            return builder.ToString();
        }
    }
}
