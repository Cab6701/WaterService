using Microsoft.AspNetCore.Mvc;

namespace WaterService.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        // Redirect to WaterInquiry page for direct customer access
        return RedirectToAction("WaterInquiry", "Account");
    }
}
