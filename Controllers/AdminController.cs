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

public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminController(ApplicationDbContext context)
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
        var admin = _context.Admins?.FirstOrDefault(a => a.Email == email && a.PasswordHash == hashedPassword);

        if (admin == null)
        {
            ViewBag.Error = "Invalid Email or Password.";
            return View();
        }

        // Ensure admin is not null before accessing properties
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, admin.Username ?? "Unknown"),
            new Claim(ClaimTypes.Email, admin.Email ?? string.Empty), // Provide a fallback value if email is null
            new Claim("AdminId", admin.AdminId.ToString()) // Custom claim for admin ID
        };

        // Create the claims identity
        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        // Sign in the user using cookie authentication
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            new AuthenticationProperties
            {
                IsPersistent = true, // Cookie persists across browser sessions
                ExpiresUtc = DateTime.UtcNow.AddHours(24) // Cookie expires in 24 hours
            }
        );

        // Redirect to Dashboard
        return RedirectToAction("Dashboard");
    }

    [Authorize]
    public IActionResult Dashboard()
    {
        return View();
    }

    [AllowAnonymous]
    public IActionResult BlogsManagement()
    {
        return View();
    }

    [Authorize]
    public IActionResult ServicesManagement()
    {
        return View();
    }

    [HttpGet]
    [Authorize]
    public IActionResult UsersManagement()
    {
        // Fetching data from the database
        var admins = _context.Admins?.ToList(); // Replace '_ccontext' with your actual DbContext instance
        return View(admins);
    }

    [HttpPost]
    [Authorize]
    public IActionResult AddAdmin(Admin admin)
    {
        if (ModelState.IsValid)
        {
            // Check if the email already exists in the database
            var existingAdmin = _context.Admins?.FirstOrDefault(a => a.Email == admin.Email);
            if (existingAdmin != null)
            {
                // If the email is already in use, add error
                TempData["ErrorMessage"] = "This email is already in use. Please choose a different one.";
                ModelState.AddModelError("Email", "This email is already in use. Please choose a different one.");
            }

            // If there are validation errors, return to the form with error messages
            if (!ModelState.IsValid)
            {
                return View("UsersManagement", _context.Admins?.ToList());
            }

            try
            {
                // Hash the password before saving
                if (!string.IsNullOrEmpty(admin.PasswordHash))
                {
                    admin.PasswordHash = HashPassword(admin.PasswordHash);
                }
                else
                {
                    TempData["ErrorMessage"] = "Password cannot be empty.";
                    return View("UsersManagement", _context.Admins?.ToList());
                }

                _context.Admins?.Add(admin);
                _context.SaveChanges();

                // Add success message to TempData
                TempData["SuccessMessage"] = "Admin added successfully!";
                return RedirectToAction("UsersManagement");
            }
            catch
            {
                // Add error message to TempData
                TempData["ErrorMessage"] = "There was an error adding the admin. Please try again.";
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

        // Return the view with the current list of admins and any error message
        return View("UsersManagement", _context.Admins?.ToList());
    }

    [HttpPost]
    public IActionResult DeleteAdmin(int id)
    {
        try
        {
            // Assuming the logged-in admin's ID is stored in the session or claims
            var loggedInAdminId = User.FindFirst("AdminId")?.Value;
            Console.WriteLine($"Logged in AdminId: {loggedInAdminId}, Deleting AdminId: {id}");

            if (loggedInAdminId != null && int.Parse(loggedInAdminId) == id)
            {
                TempData["ErrorMessage"] = "You cannot delete your own account!";
                return RedirectToAction("UsersManagement");
            } else {
                 // Replace this with your actual logic for deleting an admin by ID
                var admin = _context.Admins?.FirstOrDefault(a => a.AdminId == id);
                if (admin == null)
                {
                    TempData["ErrorMessage"] = "Admin not found!";
                    return RedirectToAction("UsersManagement");
                }

                _context.Admins?.Remove(admin);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Admin deleted successfully!";
                }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error deleting admin: {ex.Message}";
        }

        return RedirectToAction("UsersManagement");
    }

    [Authorize]
    public IActionResult ContactsManagement()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        // Sign out the user and remove their authentication cookie
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["ErrorMessage"] = "You have been logged out.";
        // Redirect to Login page
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
