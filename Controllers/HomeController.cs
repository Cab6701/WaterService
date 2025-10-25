using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WaterService.Models;

namespace WaterService.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        // Redirect to WaterInquiry page for direct customer access
        return RedirectToAction("WaterInquiry", "Account");
    }

    public IActionResult Privacy()
    {
        return View();
    }
}
