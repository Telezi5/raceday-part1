using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class HomeController : Controller
{
    private readonly ApiClient _api;
    public HomeController(ApiClient api) => _api = api;

    public async Task<IActionResult> Index(string? city, DateTime? date, string? type)
    {
        if (HttpContext.Session.GetString("Role") == "Organiser")
            return RedirectToAction("Dashboard", "Organiser");

        var qs = new List<string>();
        if (!string.IsNullOrWhiteSpace(city)) qs.Add("city=" + Uri.EscapeDataString(city.Trim()));
        if (date.HasValue) qs.Add("date=" + date.Value.ToString("yyyy-MM-dd"));
        if (!string.IsNullOrWhiteSpace(type)) qs.Add("type=" + Uri.EscapeDataString(type));
        var url = "api/events" + (qs.Count > 0 ? "?" + string.Join("&", qs) : "");

        var res = await _api.GetAsync<List<EventDto>>(url);
        if (!res.Success) TempData["Error"] = res.Error;

        var events = (res.Data ?? new())
            .Where(e => e.EventDate.Date >= DateTime.Today)
            .OrderBy(e => e.EventDate)
            .ToList();

        return View(new HomeViewModel { Events = events, City = city, Date = date, Type = type });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}