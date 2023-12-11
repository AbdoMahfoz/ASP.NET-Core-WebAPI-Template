using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
            return Redirect("swagger");
        }
}