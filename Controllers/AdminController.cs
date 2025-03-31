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
    private readonly IMongoCollection<AdminLog> _adminLogCollection;
    private readonly IMongoCollection<VisitorsLog> _visitorsLogCollection;

    public AdminController(MongoDbContext context, IMongoCollection<AdminLog> adminLogCollection, IMongoCollection<VisitorsLog> visitorsLogCollection)
    {
        _context = context;
        _adminLogCollection = adminLogCollection;
        _visitorsLogCollection = visitorsLogCollection;
    }

    [HttpGet]
    public IActionResult Login(string error)
    {
        // Remove redundant visitor logging logic
        if (User.Identity?.IsAuthenticated ?? false)
        {
            return RedirectToAction("Dashboard", "Dashboard");
        }

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

        // Retrieve and verify Turnstile response token
        string turnstileToken = Request.Form["cf-turnstile-response"].FirstOrDefault() ?? string.Empty;
        if (string.IsNullOrEmpty(turnstileToken) || !await VerifyTurnstileTokenAsync(turnstileToken))
        {
            ViewBag.Error = "Turnstile validation failed.";
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

        await _adminLogCollection.InsertOneAsync(new AdminLog
        {
            AdminId = admin.Id.ToString(),
            AdminName = admin.Username ?? "Unknown",
            Action = "Login",
            PerformedBy = admin.Username ?? "Unknown",
            Timestamp = DateTime.UtcNow
        });

        // ✅ Update visitor log to set UserType to "Admin"
        var sessionId = HttpContext.Session.GetString("SessionId");
        if (!string.IsNullOrEmpty(sessionId))
        {
            var visitor = await _visitorsLogCollection.Find(v => v.SessionId == sessionId).FirstOrDefaultAsync();
            if (visitor != null && visitor.UserType != "Admin")
            {
                visitor.UserType = "Admin";
                await _visitorsLogCollection.ReplaceOneAsync(v => v.Id == visitor.Id, visitor);
            }
        }

        return RedirectToAction("Dashboard", "Dashboard");
    }

    private async Task<bool> VerifyTurnstileTokenAsync(string token)
    {
        using (var client = new HttpClient())
        {
            var secretKey = Environment.GetEnvironmentVariable("TURNSTILE_SECRET_KEY") ?? "YOUR_TURNSTILE_SECRET_KEY";
            var postData = new Dictionary<string, string>
            {
                { "secret", secretKey },
                { "response", token }
            };
            var content = new FormUrlEncodedContent(postData);
            var response = await client.PostAsync("https://challenges.cloudflare.com/turnstile/v0/siteverify", content);
            var jsonString = await response.Content.ReadAsStringAsync();
            // DEBUG: Log the full response for inspection (remove in production)
            System.Diagnostics.Debug.WriteLine("Turnstile response: " + jsonString);
            dynamic? result = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonString);
            if (result == null)
            {
                System.Diagnostics.Debug.WriteLine("Turnstile response deserialization failed.");
                return false;
            }
            if (result.success != true)
            {
                // DEBUG: Log error codes for further details
                System.Diagnostics.Debug.WriteLine("Turnstile error codes: " + string.Join(", ", (result["error-codes"] as string[]) ?? new string[0]));
            }
            return result.success == true;
        }
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
        var adminId = User.FindFirst("AdminId")?.Value;
        var adminName = User.Identity?.Name;

        if (adminId != null && adminName != null)
        {
            await _adminLogCollection.InsertOneAsync(new AdminLog
            {
                AdminId = adminId,
                AdminName = adminName,
                Action = "Logout",
                PerformedBy = adminName,
                Timestamp = DateTime.UtcNow
            });
        }

        // Do not abandon the session; only sign out the user
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

    [HttpGet]
    [AllowAnonymous]
    public IActionResult GetUnreadMessagesCount()
    {
        int count = (int)_context.Contact.Find(m => m.IsRead == false).CountDocuments();
        System.Diagnostics.Debug.WriteLine("Unread messages count: " + count);
        return Json(new { count = count });
    }

    [HttpGet]
    [Authorize]
    public IActionResult EditProfile()
    {
        var adminId = User.FindFirst("AdminId")?.Value;
        if (string.IsNullOrEmpty(adminId))
        {
            return RedirectToAction("Login");
        }
        var admin = _context.Admins.Find(a => a.Id == new MongoDB.Bson.ObjectId(adminId)).FirstOrDefault();
        if (admin == null)
        {
            return NotFound();
        }
        return View(admin);
    }

    [HttpPost]
    [Authorize]
    public IActionResult EditProfile(Admin updatedProfile, string newPassword)
    {
        var adminId = User.FindFirst("AdminId")?.Value;
        if (string.IsNullOrEmpty(adminId))
        {
            return RedirectToAction("Login");
        }
        var admin = _context.Admins.Find(a => a.Id == new MongoDB.Bson.ObjectId(adminId)).FirstOrDefault();
        if (admin == null)
        {
            return NotFound();
        }
        // Update editable fields
        admin.Username = updatedProfile.Username;
        admin.Email = updatedProfile.Email;
        admin.Phone = updatedProfile.Phone;
        admin.Address = updatedProfile.Address;

        if (!string.IsNullOrEmpty(newPassword))
        {
            admin.PasswordHash = HashPassword(newPassword);
        }
        _context.Admins.ReplaceOne(a => a.Id == admin.Id, admin);
        TempData["SuccessMessage"] = "Profile updated successfully!";
        return RedirectToAction("EditProfile");
    }
}
