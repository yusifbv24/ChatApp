using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Web.Controllers;

public class AdminController : Controller
{
    public IActionResult OrganizationHierarchy() => View();

    public IActionResult Positions() => View();

    public IActionResult DepartmentDetails(Guid id)
    {
        ViewBag.DepartmentId = id;
        return View();
    }

    public IActionResult UserDetails(Guid id)
    {
        ViewBag.UserId = id;
        return View();
    }
}
