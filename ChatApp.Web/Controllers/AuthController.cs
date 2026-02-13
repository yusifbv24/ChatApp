using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Web.Controllers;

public class AuthController : Controller
{
    public IActionResult Login() => View();

    public IActionResult Logout() => RedirectToAction("Login");
}
