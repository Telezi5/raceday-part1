using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;

namespace RaceDay.Web.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("Role") == "Organiser")
                return RedirectToAction("Dashboard", "Organiser");
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
