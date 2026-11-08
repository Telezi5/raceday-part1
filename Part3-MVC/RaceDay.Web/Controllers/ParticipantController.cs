using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[SessionAuthorize(Role = "Participant")]
public class ParticipantController : Controller
{
    private readonly ApiClient _api;
    public ParticipantController(ApiClient api) => _api = api;

    private async Task<Dictionary<int, EventDto>> EventLookupAsync()
    {
        var res = await _api.GetAsync<List<EventDto>>("api/events");
        return (res.Data ?? new()).GroupBy(e => e.EventID).ToDictionary(g => g.Key, g => g.First());
    }

    // ---------- My Events ----------
    public async Task<IActionResult> MyEvents()
    {
        var entries = await _api.GetAsync<List<EntryDto>>("api/users/me/entries");
        if (!entries.Success) TempData["Error"] = entries.Error;
        var lookup = await EventLookupAsync();

        var rows = (entries.Data ?? new())
            .Select(e => new MyEntryRow { Entry = e, Event = lookup.GetValueOrDefault(e.EventID) })
            .OrderBy(r => r.Event?.EventDate ?? DateTime.MaxValue)
            .ToList();
        return View(rows);
    }

    [HttpPost]
    public async Task<IActionResult> CancelEntry(int id)
    {
        var res = await _api.DeleteAsync<object>($"api/entries/{id}");
        if (res.Success) TempData["Success"] = "Your enrolment has been cancelled.";
        else TempData["Error"] = res.Error ?? "We could not cancel that enrolment.";
        return RedirectToAction("MyEvents");
    }

    // ---------- My Results ----------
    public async Task<IActionResult> MyResults()
    {
        var entries = await _api.GetAsync<List<EntryDto>>("api/users/me/entries");
        if (!entries.Success) TempData["Error"] = entries.Error;
        var lookup = await EventLookupAsync();

        var totals = new Dictionary<int, int>();
        var rows = new List<MyResultRow>();
        foreach (var e in (entries.Data ?? new()).Where(x => x.Result != null))
        {
            if (!totals.ContainsKey(e.CategoryID))
            {
                var board = await _api.GetAsync<List<LeaderboardRowDto>>($"api/categories/{e.CategoryID}/results");
                totals[e.CategoryID] = board.Data?.Count ?? 0;
            }
            rows.Add(new MyResultRow
            {
                Entry = e,
                Event = lookup.GetValueOrDefault(e.EventID),
                TotalFinishers = totals[e.CategoryID]
            });
        }
        return View(rows.OrderByDescending(r => r.Event?.EventDate ?? DateTime.MinValue).ToList());
    }

    // ---------- Profile ----------
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var me = await _api.GetAsync<UserDto>("api/users/me");
        if (!me.Success || me.Data == null)
        {
            TempData["Error"] = me.Error ?? "We could not load your profile.";
            return RedirectToAction("Index", "Home");
        }
        return View(new ProfileViewModel
        {
            FullName = me.Data.FullName,
            ContactNumber = me.Data.ContactNumber,
            Email = me.Data.Email,
            Role = me.Data.Role,
            DateRegistered = me.Data.DateRegistered
        });
    }

    [HttpPost]
    public async Task<IActionResult> Profile(ProfileViewModel m)
    {
        if (!ModelState.IsValid) return View(m);
        var res = await _api.PutAsync<UserDto>("api/users/me", new { m.FullName, m.ContactNumber });
        if (!res.Success)
        {
            ModelState.AddModelError("", res.Error ?? "We could not update your profile.");
            return View(m);
        }
        HttpContext.Session.SetString("FullName", m.FullName);
        TempData["Success"] = "Profile updated.";
        return RedirectToAction("Profile");
    }
}