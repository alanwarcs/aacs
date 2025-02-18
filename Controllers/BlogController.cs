using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using aacs.Models;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using MongoDB.Driver;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace aacs.Controllers
{
    public class BlogController : Controller
    {
        private readonly MongoDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly Cloudinary _cloudinary;

        public BlogController(MongoDbContext context, IWebHostEnvironment webHostEnvironment, Cloudinary cloudinary)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _cloudinary = cloudinary;
        }
        [Authorize]
        public IActionResult BlogsManagement(int page = 1)
        {
            const int pageSize = 10;
            var blogs = _context.Blog.Find(_ => true)
                                     .SortBy(b => b.Id)
                                     .Skip((page - 1) * pageSize)
                                     .Limit(pageSize)
                                     .ToList();

            var totalBlogs = _context.Blog.CountDocuments(_ => true);
            var totalPages = (int)Math.Ceiling(totalBlogs / (double)pageSize);

            var model = new PaginatedList<Blog>(blogs, (int)totalBlogs, page, pageSize);

            ViewData["TotalPages"] = totalPages;
            ViewData["CurrentPage"] = page;

            return View("~/Views/Admin/BlogsManagement.cshtml", model);
        }

        [HttpGet]
        [Authorize]
        public IActionResult GetBlogDetails(string id)
        {
            if (!MongoDB.Bson.ObjectId.TryParse(id, out var objectId))
            {
                return BadRequest(new { message = "Invalid blog ID format." });
            }

            var blog = _context.Blog.Find(b => b.Id == objectId).FirstOrDefault();

            if (blog == null)
            {
                return NotFound(new { message = "Blog not found." });
            }

            return Json(new
            {
                blogId = blog.Id.ToString(),  // Convert ObjectId back to string
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
        public async Task<IActionResult> AddBlog(Blog blog, IFormFile HeaderImage)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Upload to Cloudinary
                    if (HeaderImage != null && HeaderImage.Length > 0)
                    {
                        using var stream = HeaderImage.OpenReadStream();
                        var uploadParams = new ImageUploadParams
                        {
                            File = new FileDescription(HeaderImage.FileName, stream),
                            Folder = "blog_images"  // Cloudinary folder
                        };

                        var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                        blog.HeaderImageUrl = uploadResult.SecureUrl.ToString();
                    }

                    if (blog.Status == "Published")
                    {
                        blog.DatePublished = DateTime.Now;
                    }

                    _context.Blog.InsertOne(blog);

                    TempData["SuccessMessage"] = "New blog added successfully!";
                    return RedirectToAction("BlogsManagement");
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Error while adding blog: " + ex.Message;
                    return RedirectToAction("BlogsManagement");
                }
            }

            TempData["ValidationErrors"] = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return View("~/Views/Admin/BlogsManagement.cshtml");
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> DeleteBlog(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    TempData["ErrorMessage"] = "Blog ID is required.";
                    return RedirectToAction("BlogsManagement");
                }

                var blog = _context.Blog.Find(b => b.Id == new MongoDB.Bson.ObjectId(id)).FirstOrDefault();
                if (blog == null)
                {
                    TempData["ErrorMessage"] = "Blog not found!";
                    return RedirectToAction("BlogsManagement");
                }

                // Extract public_id from Cloudinary URL if it exists
                var imageUrl = blog.HeaderImageUrl;
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    try
                    {
                        var publicId = Path.GetFileNameWithoutExtension(new Uri(imageUrl).AbsolutePath);

                        // Delete image from Cloudinary
                        var deletionParams = new DeletionParams(publicId);
                        await _cloudinary.DestroyAsync(deletionParams);
                    }
                    catch (UriFormatException)
                    {
                        TempData["ErrorMessage"] = "Invalid image URL format.";
                        return RedirectToAction("BlogsManagement");
                    }
                }

                _context.Blog.DeleteOne(b => b.Id == new MongoDB.Bson.ObjectId(id));
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
        public async Task<IActionResult> UpdateBlog(string Id, string Title, string Author, string Content, string Tags, string Status, string Description, IFormFile? HeaderImage)
        {

            var blog = _context.Blog.Find(b => b.Id == new MongoDB.Bson.ObjectId(Id)).FirstOrDefault();
            
            if (blog == null)
            {
                TempData["ErrorMessage"] = "Blog not found.";
                return RedirectToAction("BlogsManagement");
            }

            blog.Title = Title;
            blog.Author = Author;
            blog.Content = Content;
            blog.Tags = Tags;
            blog.Status = Status;
            blog.Description = Description;

            // Upload new image if provided
            if (HeaderImage != null && HeaderImage.Length > 0)
            {
                using var stream = HeaderImage.OpenReadStream();
                Console.WriteLine(HeaderImage.FileName);
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(HeaderImage.FileName, stream),
                    Folder = "blog_images"
                };
                Console.WriteLine(uploadParams);
                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                Console.WriteLine(uploadResult);
                blog.HeaderImageUrl = uploadResult.SecureUrl.ToString();
            }
            if (Status == "Published" && blog.DatePublished == null)
            {
                blog.DatePublished = DateTime.Now;
            }
            else if (Status == "Draft")
            {
                blog.DatePublished = null;
            }

            var update = Builders<Blog>.Update
                .Set(b => b.Title, blog.Title)
                .Set(b => b.Author, blog.Author)
                .Set(b => b.Content, blog.Content)
                .Set(b => b.Tags, blog.Tags)
                .Set(b => b.Status, blog.Status)
                .Set(b => b.Description, blog.Description)
                .Set(b => b.HeaderImageUrl, blog.HeaderImageUrl)
                .Set(b => b.DatePublished, blog.DatePublished);

            _context.Blog.UpdateOne(b => b.Id == blog.Id, update);

            TempData["SuccessMessage"] = "Blog updated successfully!";
            return RedirectToAction("BlogsManagement");
        }
    }
}