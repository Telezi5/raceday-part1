using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api")]
public class EntriesController : ControllerBase
{
    private readonly RaceDayContext _db;

    public EntriesController(RaceDayContext db) => _db = db;

    private int? CurrentUserId => HttpContext.Session.GetInt32("UserId");

    /// <summary>Enters the logged-in Participant into a race category and generates a bib number.</summary>
    /// <remarks>
    /// The enrolment links the Participant, the category and (through the category) the event.
    /// The body is optional: paymentStatus can be Pending (default), Paid or Refunded.
    /// </remarks>
    /// <param name="categoryId">The category's ID.</param>
    /// <response code="201">Enrolled. Returns the entry with its bib number.</response>
    /// <response code="400">Invalid payment status, or the event is not open for entries.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is not a Participant.</response>
    /// <response code="404">The category does not exist.</response>
    /// <response code="409">Already entered, or the category is full.</response>
    [HttpPost("categories/{categoryId:int}/entries")]
    [SessionAuthorize(Role = "Participant")]
    [ProducesResponseType(typeof(EntryResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enrol(int categoryId, EntryRequestDto? dto)
    {
        var category = await _db.Categories.Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryID == categoryId);

        if (category == null)
            return NotFound(new { message = "Category not found." });
        if (category.Event.Status != "Published")
            return BadRequest(new { message = "Entries are only open for published events." });

        var userId = CurrentUserId!.Value;

        if (await _db.Entries.AnyAsync(e => e.ParticipantID == userId && e.CategoryID == categoryId))
            return Conflict(new { message = "You have already entered this category." });

        var taken = await _db.Entries.CountAsync(e => e.CategoryID == categoryId);
        if (taken >= category.MaxParticipants)
            return Conflict(new { message = "This category is full." });

        var entry = new Entry
        {
            ParticipantID = userId,
            CategoryID = categoryId,
            BibNumber = Guid.NewGuid().ToString("N")[..10],   // temporary, replaced below
            PaymentStatus = dto?.PaymentStatus ?? "Pending"
        };

        try
        {
            _db.Entries.Add(entry);
            await _db.SaveChangesAsync();

            entry.BibNumber = $"RD{entry.EntryID:D6}";
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "You have already entered this category." });
        }

        await _db.Entry(entry).Reference(e => e.Participant).LoadAsync();

        return Created($"/api/entries/{entry.EntryID}", EntryResponseDto.From(entry));
    }

    /// <summary>Lists every entry across an event. Only the Organiser who owns the event may view this.</summary>
    /// <param name="eventId">The event's ID.</param>
    /// <response code="200">Array of entries with participant, category and result details.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is not the owning Organiser.</response>
    /// <response code="404">The event does not exist.</response>
    [HttpGet("events/{eventId:int}/entries")]
    [SessionAuthorize(Role = "Organiser")]
    [ProducesResponseType(typeof(List<EntryResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventID == eventId);
        if (ev == null)
            return NotFound(new { message = "Event not found." });
        if (ev.OrganiserID != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only view entries for your own events." });

        var entries = await _db.Entries
            .Include(e => e.Participant)
            .Include(e => e.Category).ThenInclude(c => c.Event)
            .Include(e => e.Result)
            .Where(e => e.Category.EventID == eventId)
            .OrderBy(e => e.EntryID)
            .ToListAsync();

        return Ok(entries.Select(EntryResponseDto.From));
    }

    /// <summary>Cancels the logged-in Participant's own entry.</summary>
    /// <remarks>An entry that already has a recorded result cannot be cancelled.</remarks>
    /// <param name="id">The entry's ID.</param>
    /// <response code="204">Entry cancelled.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">The entry belongs to someone else, or the caller is not a Participant.</response>
    /// <response code="404">The entry does not exist.</response>
    /// <response code="409">A result has already been recorded for this entry.</response>
    [HttpDelete("entries/{id:int}")]
    [SessionAuthorize(Role = "Participant")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(int id)
    {
        var entry = await _db.Entries.Include(e => e.Result)
            .FirstOrDefaultAsync(e => e.EntryID == id);

        if (entry == null)
            return NotFound(new { message = "Entry not found." });
        if (entry.ParticipantID != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only cancel your own entries." });
        if (entry.Result != null)
            return Conflict(new { message = "An entry with a recorded result cannot be cancelled." });

        _db.Entries.Remove(entry);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}