using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using aacs.Models;

namespace aacs.Controllers;

public class BlogController : Controller
{
    [Authorize]
    public IActionResult BlogsManagement()
    {
        return View("~/Views/Admin/BlogsManagement.cshtml");
    }
}