using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Web.Controllers;

public class MessagesController : Controller
{
    public IActionResult Index() => View();
}
