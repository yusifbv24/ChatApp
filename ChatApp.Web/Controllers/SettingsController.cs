using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Web.Controllers;

public class SettingsController : Controller
{
    public IActionResult Index() => View();
}
