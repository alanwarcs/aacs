using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using aacs.Models;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;

namespace aacs.Controllers
{
    public class BlogController : Controller
    {
        private readonly ApplicationDbContext _context;

        // Constructor to inject ApplicationDbContext
        public BlogController(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        [Authorize]
        public IActionResult BlogsManagement(int page = 1)
        {
            const int pageSize = 2; 
            var blogs = _context.Blog?.OrderBy(b => b.BlogId) // Sort by ID or another field if needed
                                    .Skip((page - 1) * pageSize)
                                    .Take(pageSize)
                                    .ToList() ?? new List<Blog>(); // Fallback to an empty list if null

            var totalBlogs= _context.Blog?.Count() ?? 0;
            var totalPages = (int)Math.Ceiling(totalBlogs / (double)pageSize);

            var model = new PaginatedList<Blog>(blogs, totalBlogs, page, pageSize);

            ViewData["TotalPages"] = totalPages; // Pass total pages to the view
            ViewData["CurrentPage"] = page; // Pass current page to the view

            return View("~/Views/Admin/BlogsManagement.cshtml", model); // Use the correct view path
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddBlog(IFormFile HeaderImage, string Title, string Author, string Content, string Tags, string Status, string Description)
        {
            if (!ModelState.IsValid)
            {
                Console.WriteLine("AddBlog method hit");
                TempData["ValidationErrors"] = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return RedirectToAction("BlogsManagement");
            }

            if (string.IsNullOrEmpty(Content))
            {
                Console.WriteLine("Blog content is empty");
                ModelState.AddModelError("Content", "Blog content is required.");
                TempData["ValidationErrors"] = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return RedirectToAction("BlogsManagement");
            }

            if (string.IsNullOrEmpty(Description))
            {
                ModelState.AddModelError("Description", "Blog description is required.");
                TempData["ValidationErrors"] = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return RedirectToAction("BlogsManagement");
            }

            string? imagePath = null;
            if (HeaderImage != null && HeaderImage.Length > 0)
            {
                var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
                Directory.CreateDirectory(uploads); // Ensure the directory exists
                var filePath = Path.Combine(uploads, HeaderImage.FileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await HeaderImage.CopyToAsync(fileStream);
                }
                imagePath = "/images/blogs/" + HeaderImage.FileName;
            }

            var newBlog = new Blog
            {
                Title = Title,
                Author = Author,
                Content = Content,
                Tags = Tags,
                Status = Status,
                DatePublished = Status == "Published" ? DateTime.Now : (DateTime?)null,
                HeaderImageUrl = imagePath,
                DateCreated = DateTime.Now,
            };

            if (_context.Blog != null)
            {
                _context.Blog.Add(newBlog);
                await _context.SaveChangesAsync();
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Unable to add blog. Blog context is null.");
                TempData["ValidationErrors"] = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return RedirectToAction("BlogsManagement");
            }

            TempData["SuccessMessage"] = "Blog added successfully!";
            return RedirectToAction("BlogsManagement");
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
            });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UpdateBlog(IFormFile? headerImage, int blogId, string title, string author, string content, string tags, string status, string description)
        {
            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine(error.ErrorMessage);
                }
                return View("~/Views/Admin/BlogsManagement.cshtml");
            }

            var blog = _context.Blog?.FirstOrDefault(b => b.BlogId == blogId);
            if (blog == null)
            {
                ModelState.AddModelError(string.Empty, "Blog not found.");
                return View("~/Views/Admin/BlogsManagement.cshtml");
            }
            if (string.IsNullOrEmpty(content))
            {
                ModelState.AddModelError("content", "Blog content is required.");
                return View("~/Views/Admin/BlogsManagement.cshtml");
            }

            if (string.IsNullOrEmpty(description))
            {
                ModelState.AddModelError("description", "Blog description is required.");
                return View("~/Views/Admin/BlogsManagement.cshtml");
            }

            string? imagePath = blog.HeaderImageUrl;
            if (headerImage != null && headerImage.Length > 0)
            {
                var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
                Directory.CreateDirectory(uploads); // Ensure the directory exists
                var filePath = Path.Combine(uploads, headerImage.FileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await headerImage.CopyToAsync(fileStream);
                }
                imagePath = "/images/" + headerImage.FileName;
            }

            blog.Title = title;
            blog.Author = author;
            blog.Content = content;
            blog.Tags = tags;
            blog.Status = status;
            blog.HeaderImageUrl = imagePath;
            blog.DatePublished = status == "Published" ? DateTime.Now : blog.DatePublished;

            await _context.SaveChangesAsync();

            return RedirectToAction("BlogsManagement");
        }
    }
}