using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[SessionAuthorize(Role = "Organiser")]
public class OrganiserController : Controller
{
    private readonly ApiClient _api;
    public OrganiserController(ApiClient api) => _api = api;

    private int MyId => HttpContext.Session.GetInt32("UserId") ?? 0;

    private async Task<List<EventDto>> MyEventsAsync()
    {
        var res = await _api.GetAsync<List<EventDto>>("api/events");
        if (!res.Success) { TempData["Error"] = res.Error; return new(); }
        return (res.Data ?? new()).Where(e => e.OrganiserID == MyId).OrderBy(e => e.EventDate).ToList();
    }

    private async Task<EventDto?> OwnedEventAsync(int id)
    {
        var res = await _api.GetAsync<EventDto>($"api/events/{id}");
        if (!res.Success || res.Data == null || res.Data.OrganiserID != MyId) return null;
        return res.Data;
    }

    private IActionResult NotOwned()
    {
        TempData["Error"] = "We could not find that event.";
        return RedirectToAction("Events");
    }

    private static object ToRequest(EventFormViewModel m) => new
    {
        m.EventName,
        m.EventType,
        EventDate = m.EventDate.ToString("yyyy-MM-dd"),
        m.City,
        m.DistanceKM,
        m.Description,
        m.Status,
        Venue = new { m.StartPoint, m.EndPoint, m.RouteDescription, ElevationGainM = m.ElevationGainM ?? 0 }
    };

    // ---------- Dashboard ----------
    public async Task<IActionResult> Dashboard()
    {
        var events = await MyEventsAsync();
        var vm = new DashboardViewModel();
        foreach (var e in events)
        {
            var entries = await _api.GetAsync<List<EntryDto>>($"api/events/{e.EventID}/entries");
            vm.Events.Add(new EventSummary { Event = e, Enrolments = entries.Data?.Count ?? 0 });
        }
        return View(vm);
    }

    // ---------- Events ----------
    public async Task<IActionResult> Events() => View(await MyEventsAsync());

    [HttpGet]
    public IActionResult CreateEvent() => View("EventForm", new EventFormViewModel());

    [HttpPost]
    public async Task<IActionResult> CreateEvent(EventFormViewModel m)
    {
        if (!ModelState.IsValid) return View("EventForm", m);
        var res = await _api.PostAsync<EventDto>("api/events", ToRequest(m));
        if (!res.Success)
        {
            ModelState.AddModelError("", res.Error ?? "Could not create the event.");
            return View("EventForm", m);
        }

        if (m.Banner != null && m.Banner.Length > 0 && res.Data != null)
        {
            var up = await _api.PostFileAsync<EventDto>($"api/events/{res.Data.EventID}/banner", m.Banner);
            if (!up.Success) TempData["Error"] = "The event was created, but the banner could not be uploaded: " + up.Error;
        }

        TempData["Success"] = "Event created.";
        return RedirectToAction("Events");
    }

    [HttpGet]
    public async Task<IActionResult> EditEvent(int id)
    {
        var e = await OwnedEventAsync(id);
        if (e == null) return NotOwned();
        return View("EventForm", new EventFormViewModel
        {
            EventID = e.EventID,
            EventName = e.EventName,
            EventType = e.EventType,
            EventDate = e.EventDate,
            City = e.City,
            DistanceKM = e.DistanceKM,
            Description = e.Description,
            Status = e.Status ?? "Published",
            StartPoint = e.Venue?.StartPoint ?? "",
            EndPoint = e.Venue?.EndPoint ?? "",
            RouteDescription = e.Venue?.RouteDescription,
            ElevationGainM = e.Venue?.ElevationGainM,
            BannerUrl = e.BannerUrl
        });
    }

    [HttpPost]
    public async Task<IActionResult> EditEvent(EventFormViewModel m)
    {
        if (!ModelState.IsValid) return View("EventForm", m);
        var res = await _api.PutAsync<EventDto>($"api/events/{m.EventID}", ToRequest(m));
        if (!res.Success)
        {
            ModelState.AddModelError("", res.Error ?? "Could not update the event.");
            return View("EventForm", m);
        }

        if (m.Banner != null && m.Banner.Length > 0)
        {
            var up = await _api.PostFileAsync<EventDto>($"api/events/{m.EventID}/banner", m.Banner);
            if (!up.Success) TempData["Error"] = "The event was updated, but the banner could not be uploaded: " + up.Error;
        }

        TempData["Success"] = "Event updated.";
        return RedirectToAction("Events");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var res = await _api.DeleteAsync<object>($"api/events/{id}");
        if (res.Success) TempData["Success"] = "Event deleted.";
        else TempData["Error"] = res.Error ?? "Could not delete the event.";
        return RedirectToAction("Events");
    }

    // ---------- Categories ----------
    public async Task<IActionResult> Categories(int eventId)
    {
        var ev = await OwnedEventAsync(eventId);
        if (ev == null) return NotOwned();
        var cats = await _api.GetAsync<List<CategoryDto>>($"api/events/{eventId}/categories");
        return View(new EventCategoriesViewModel { Event = ev, Categories = cats.Data ?? new() });
    }

    [HttpPost]
    public async Task<IActionResult> AddCategory(int eventId, CategoryFormViewModel m)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please enter a category name.";
            return RedirectToAction("Categories", new { eventId });
        }
        var res = await _api.PostAsync<CategoryDto>($"api/events/{eventId}/categories", m);
        if (res.Success) TempData["Success"] = "Category added.";
        else TempData["Error"] = res.Error ?? "Could not add the category.";
        return RedirectToAction("Categories", new { eventId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCategory(int id, int eventId)
    {
        var res = await _api.DeleteAsync<object>($"api/categories/{id}");
        if (res.Success) TempData["Success"] = "Category removed.";
        else TempData["Error"] = res.Error ?? "Could not remove the category.";
        return RedirectToAction("Categories", new { eventId });
    }

    // ---------- Enrolments ----------
    public async Task<IActionResult> Enrolments(int eventId)
    {
        var ev = await OwnedEventAsync(eventId);
        if (ev == null) return NotOwned();
        var entries = await _api.GetAsync<List<EntryDto>>($"api/events/{eventId}/entries");
        return View(new EventEntriesViewModel { Event = ev, Entries = entries.Data ?? new() });
    }

    // ---------- Results ----------
    public async Task<IActionResult> Results(int eventId)
    {
        var ev = await OwnedEventAsync(eventId);
        if (ev == null) return NotOwned();
        var entries = await _api.GetAsync<List<EntryDto>>($"api/events/{eventId}/entries");
        return View(new EventEntriesViewModel { Event = ev, Entries = entries.Data ?? new() });
    }

    [HttpPost]
    public async Task<IActionResult> SaveResult(int eventId, int entryId, int? resultId, string? finishTime, int? position, string status)
    {
        var body = new
        {
            finishTime = string.IsNullOrWhiteSpace(finishTime) ? null : finishTime.Trim(),
            position,
            status
        };

        var res = (resultId.HasValue && resultId > 0)
            ? await _api.PutAsync<ResultDto>($"api/results/{resultId}", body)
            : await _api.PostAsync<ResultDto>($"api/entries/{entryId}/result", body);

        if (res.Success) TempData["Success"] = "Result saved.";
        else TempData["Error"] = res.Error ?? "Could not save the result.";
        return RedirectToAction("Results", new { eventId });
    }
}