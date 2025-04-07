using Microsoft.AspNetCore.Authentication.Cookies;
using CloudinaryDotNet;
using DotNetEnv;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver; // Add this using statement
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Http;

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

// Register IMongoDatabase using MongoDbContext
builder.Services.AddSingleton<IMongoDatabase>(sp =>
{
    var mongoDbContext = sp.GetRequiredService<MongoDbContext>();
    return mongoDbContext.Database;
});

builder.Services.AddSingleton<IMongoCollection<VisitorsLog>>(sp =>
{
    var database = sp.GetRequiredService<IMongoDatabase>();
    return database.GetCollection<VisitorsLog>("VisitorsLog");
});

builder.Services.AddSingleton<IMongoCollection<AdminLog>>(sp =>
{
    var database = sp.GetRequiredService<IMongoDatabase>();
    return database.GetCollection<AdminLog>("AdminLog");
});

// Add HttpClientFactory
builder.Services.AddHttpClient();

// Add MemoryCache
builder.Services.AddMemoryCache();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add authentication using cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        // Optional Access Denied page
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.Redirect("/Admin/Login");
            return Task.CompletedTask;
        };
    });

builder.Services.AddControllers();
builder.Services.AddLogging();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.Use(async (context, next) =>
{
    var allowedHosts = new[] { "alanwar.studio", "www.alanwar.studio" };
    var requestHost = context.Request.Host.Host.ToLower();

    if (!allowedHosts.Contains(requestHost))
    {
        context.Response.StatusCode = 403;
        await context.Response.WriteAsync("Access Denied");
        return;
    }
    await next();
});

app.UseRouting();

// Enable session middleware
app.UseSession();

// Add middleware to enable JSON request handling
app.Use(async (context, next) =>
{
    if (context.Request.ContentType == "application/json")
    {
        context.Request.EnableBuffering();
    }
    await next();
});

// Add visitor tracking middleware
app.UseMiddleware<VisitorTrackingMiddleware>();
app.UseMiddleware<BotDetectionMiddleware>();

// Update the default route for Access Denied
app.MapControllerRoute(
    name: "accessDenied",
    pattern: "AccessDenied",
    defaults: new { controller = "Home", action = "AccessDenied" });

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
    defaults: new { controller = "Admin", action = "Login" });

app.MapControllerRoute(
    name: "admin",
    pattern: "Admin/{controller=Dashboard}/{action=Index}/{id?}");

app.Run();