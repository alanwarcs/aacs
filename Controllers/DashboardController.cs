using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using aacs.Models;

namespace aacs.Controllers;

public class DashboardController : Controller
{
    [Authorize]
    public IActionResult Dashboard()
    {
        return View("~/Views/Admin/Dashboard.cshtml");
    }
}