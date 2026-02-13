using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();
}
