using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class EventsController : Controller
{
    private readonly ApiClient _api;
    public EventsController(ApiClient api) => _api = api;

    public async Task<IActionResult> Details(int id)
    {
        var ev = await _api.GetAsync<EventDto>($"api/events/{id}");
        if (!ev.Success || ev.Data == null)
        {
            TempData["Error"] = ev.Error ?? "We could not find that event.";
            return RedirectToAction("Index", "Home");
        }
        var cats = await _api.GetAsync<List<CategoryDto>>($"api/events/{id}/categories");
        return View(new EventDetailsViewModel { Event = ev.Data, Categories = cats.Data ?? new() });
    }

    [HttpPost]
    [SessionAuthorize(Role = "Participant")]
    public async Task<IActionResult> Enrol(int eventId, int categoryId)
    {
        var res = await _api.PostAsync<EntryDto>($"api/categories/{categoryId}/entries", new { });
        if (res.Success)
        {
            TempData["Success"] = "You’re in! Your enrolment has been recorded.";
            return RedirectToAction("MyEvents", "Participant");
        }
        TempData["Error"] = res.Error ?? "We could not enrol you. Please try again.";
        return RedirectToAction("Details", new { id = eventId });
    }
}