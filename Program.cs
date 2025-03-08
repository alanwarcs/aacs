using Microsoft.AspNetCore.Authentication.Cookies;
using CloudinaryDotNet;
using DotNetEnv;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Load environment variables from .env file
Env.Load();

// Read Cloudinary credentials from environment variables
var cloudinaryAccount = new Account(
    Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME"),
    Environment.GetEnvironmentVariable("CLOUDINARY_API_KEY"),
    Environment.GetEnvironmentVariable("CLOUDINARY_API_SECRET")
);

var cloudinary = new Cloudinary(cloudinaryAccount);
builder.Services.AddSingleton(cloudinary);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register MongoDB context
builder.Services.AddSingleton<MongoDbContext>();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Set session timeout
    options.Cookie.HttpOnly = true; // Ensure session cookie is only accessible via HTTP
    options.Cookie.IsEssential = true; // Ensure session cookie is always stored
});

// Add authentication using cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Admin/Login"; // Redirect to Login if unauthenticated
        options.AccessDeniedPath = "/Admin/AccessDenied"; // Optional Access Denied page
        options.Events.OnRedirectToLogin = context =>
        {
            context.HttpContext.Response.Redirect("/Admin/Login?error=Access Denied!You need to be logged in to view this page.");
            return Task.CompletedTask;
        };
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Enable session middleware
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Default routes for public pages
app.MapControllerRoute(
    name: "default",
    pattern: "{action=Index}",
    defaults: new { controller = "Home" });
// Admin routes
app.MapControllerRoute(
    name: "admin",
    pattern: "Admin/Login",
    defaults: new { controller = "Admin", action="Login" });

app.MapControllerRoute(
    name: "admin",
    pattern: "Admin/{controller=Dashboard}/{action=Index}/{id?}");

app.Run();