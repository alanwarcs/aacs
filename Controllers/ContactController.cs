using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using aacs.Models;

namespace aacs.Controllers;

public class ContactController : Controller
{
    [Authorize]
    public IActionResult ContactsManagement()
    {
        return View("~/Views/Admin/ContactsManagement.cshtml");
    }
}