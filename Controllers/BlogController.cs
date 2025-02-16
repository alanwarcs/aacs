using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using aacs.Models;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace aacs.Controllers
{
    public class BlogController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        // Constructor to inject ApplicationDbContext
        public BlogController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _webHostEnvironment = webHostEnvironment;
        }

        [Authorize]
        public IActionResult BlogsManagement(int page = 1)
        {
            const int pageSize = 10;
            var blogs = _context.Blog?.OrderBy(b => b.BlogId) // Sort by ID or another field if needed
                                    .Skip((page - 1) * pageSize)
                                    .Take(pageSize)
                                    .ToList() ?? new List<Blog>(); // Fallback to an empty list if null

            var totalBlogs = _context.Blog?.Count() ?? 0;
            var totalPages = (int)Math.Ceiling(totalBlogs / (double)pageSize);

            var model = new PaginatedList<Blog>(blogs, totalBlogs, page, pageSize);

            ViewData["TotalPages"] = totalPages; // Pass total pages to the view
            ViewData["CurrentPage"] = page; // Pass current page to the view

            return View("~/Views/Admin/BlogsManagement.cshtml", model); // Use the correct view path
        }

        [HttpGet]
        [Authorize]
        public IActionResult GetBlogDetails(int id)
        {
            var blog = _context.Blog?.FirstOrDefault(b => b.BlogId == id);
            if (blog == null)
            {
                return NotFound(new { message = "Blog not found." });
            }

            return Json(new
            {
                blogId = blog.BlogId,
                title = blog.Title,
                author = blog.Author,
                content = blog.Content,
                tags = blog.Tags,
                status = blog.Status,
                description = blog.Description,
                datePublished = blog.DatePublished,
                headerImageUrl = blog.HeaderImageUrl
            });
        }

        [HttpPost]
        [Authorize]
        public IActionResult AddBlog(Blog blog, IFormFile HeaderImage)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Handle Image Upload
                    if (HeaderImage != null && HeaderImage.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "blog");
                        string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(HeaderImage.FileName);
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        // Ensure the directory exists
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            HeaderImage.CopyTo(fileStream);
                        }

                        blog.HeaderImageUrl = "/images/blog/" + uniqueFileName;
                    }

                    // Set Published Date if Status is "Published"
                    if (blog.Status == "Published")
                    {
                        blog.DatePublished = DateTime.Now;
                    }

                    _context?.Blog?.Add(blog);
                    _context?.SaveChanges();

                    TempData["SuccessMessage"] = "New blog added successfully!";
                    return RedirectToAction("BlogsManagement");
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "An error occurred while adding the blog. Error: " + ex.Message;
                    return RedirectToAction("BlogsManagement");
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

            // Return the view with the correct model type
            const int pageSize = 2;
            var blogs = _context.Blog?.OrderBy(b => b.BlogId)
                                    .Take(pageSize)
                                    .ToList() ?? new List<Blog>();
            var totalBlogs = _context.Blog?.Count() ?? 0;
            var model = new PaginatedList<Blog>(blogs, totalBlogs, 1, pageSize);

            return View("~/Views/Admin/BlogsManagement.cshtml", model);
        }

        [HttpPost]
        [Authorize]
        public IActionResult DeleteBlog(int id)
        {
            try
            {
                var blog = _context.Blog?.FirstOrDefault(b => b.BlogId == id);
                if (blog == null)
                {
                    TempData["ErrorMessage"] = "Blog not found!";
                    return RedirectToAction("BlogsManagement");
                }

                _context.Blog?.Remove(blog);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Blog deleted successfully!";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "There was an error deleting the Blog. Please try again.";
            }

            return RedirectToAction("BlogsManagement");
        }

        [HttpPost]
        [Authorize]
        public IActionResult UpdateBlog(int blogId, string Title, string Author, string Content, string Tags, string Status, string Description, IFormFile? HeaderImage)
        {
            var blog = _context.Blog?.FirstOrDefault(b => b.BlogId == blogId);
            if (blog == null)
            {
                TempData["ErrorMessage"] = "Blog not found.";
                return RedirectToAction("BlogsManagement");
            }

            // Update blog details
            blog.Title = Title;
            blog.Author = Author;
            blog.Content = Content;
            blog.Tags = Tags;
            blog.Status = Status;
            blog.Description = Description;

            // Handle image upload
            if (HeaderImage != null && HeaderImage.Length > 0)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "blog");
                string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(HeaderImage.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Ensure the directory exists
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Save the new image
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    HeaderImage.CopyTo(fileStream);
                }

                // Update image URL
                blog.HeaderImageUrl = "/images/blog/" + uniqueFileName;
            }
            else if (string.IsNullOrEmpty(blog.HeaderImageUrl))
            {
                TempData["ErrorMessage"] = "Header image is required.";
                return RedirectToAction("BlogsManagement");
            }

            // Update DatePublished when the status is 'Published'
            if (Status == "Published" && blog.DatePublished == null)
            {
                blog.DatePublished = DateTime.Now;
            }
            else if (Status == "Draft")
            {
                blog.DatePublished = null; // Reset if reverting to draft
            }

            // Save changes to database
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Blog updated successfully!";
            return RedirectToAction("BlogsManagement");
        }
    }
}