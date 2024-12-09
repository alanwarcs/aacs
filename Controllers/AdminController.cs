using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using aacs.Models;

namespace aacs.Controllers;

public class AdminController : Controller
{
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    public IActionResult Dashboard()
    {
        return View();
    }

}