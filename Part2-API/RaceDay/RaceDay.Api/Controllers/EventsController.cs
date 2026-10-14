using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly RaceDayContext _db;

    public EventsController(RaceDayContext db) => _db = db;

    private int? CurrentUserId => HttpContext.Session.GetInt32("UserId");
    private string? CurrentRole => HttpContext.Session.GetString("Role");

    /// <summary>Lists events. Draft events are hidden unless they belong to the logged-in Organiser.</summary>
    /// <remarks>Optional filters: city (partial match), date (yyyy-MM-dd) and type (Run, Walk or Cycle).</remarks>
    /// <response code="200">Array of events, ordered by date.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<EventResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] string? city, [FromQuery] DateOnly? date, [FromQuery] string? type)
    {
        var query = _db.Events.Include(e => e.Organiser).Include(e => e.Venue).AsQueryable();

        if (CurrentRole == "Organiser" && CurrentUserId != null)
        {
            var myId = CurrentUserId.Value;
            query = query.Where(e => e.Status != "Draft" || e.OrganiserID == myId);
        }
        else
        {
            query = query.Where(e => e.Status != "Draft");
        }

        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(e => e.City.Contains(city));
        if (date != null)
            query = query.Where(e => e.EventDate == date.Value);
        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(e => e.EventType == type);

        var events = await query.OrderBy(e => e.EventDate).ToListAsync();
        return Ok(events.Select(EventResponseDto.From));
    }

    /// <summary>Returns the full details of one event, including its venue and route information.</summary>
    /// <param name="id">The event's ID.</param>
    /// <response code="200">The event.</response>
    /// <response code="404">The event does not exist (or is a draft owned by someone else).</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var ev = await _db.Events.Include(e => e.Organiser).Include(e => e.Venue)
            .FirstOrDefaultAsync(e => e.EventID == id);

        if (ev == null || (ev.Status == "Draft" && ev.OrganiserID != CurrentUserId))
            return NotFound(new { message = "Event not found." });

        return Ok(EventResponseDto.From(ev));
    }

    /// <summary>Creates a new event. The logged-in Organiser becomes the event's owner.</summary>
    /// <remarks>
    /// Expects eventName, eventType (Run, Walk or Cycle), eventDate, city, distanceKM, description and status.
    /// A venue (startPoint, endPoint, routeDescription, elevationGainM) can be included.
    /// </remarks>
    /// <response code="201">Event created.</response>
    /// <response code="400">Validation error.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is not an Organiser.</response>
    [HttpPost]
    [SessionAuthorize(Role = "Organiser")]
    [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(EventRequestDto dto)
    {
        var ev = new Event
        {
            OrganiserID = CurrentUserId!.Value,
            EventName = dto.EventName.Trim(),
            EventType = dto.EventType,
            EventDate = dto.EventDate!.Value,
            City = dto.City.Trim(),
            DistanceKM = dto.DistanceKM,
            Description = dto.Description,
            Status = dto.Status
        };

        if (dto.Venue != null)
        {
            ev.Venue = new Venue
            {
                StartPoint = dto.Venue.StartPoint,
                EndPoint = dto.Venue.EndPoint,
                RouteDescription = dto.Venue.RouteDescription,
                ElevationGainM = dto.Venue.ElevationGainM
            };
        }

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();
        await _db.Entry(ev).Reference(e => e.Organiser).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = ev.EventID }, EventResponseDto.From(ev));
    }

    /// <summary>Updates an event. Only the Organiser who created it may edit it.</summary>
    /// <param name="id">The event's ID.</param>
    /// <response code="200">Event updated.</response>
    /// <response code="400">Validation error.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is not the owning Organiser.</response>
    /// <response code="404">The event does not exist.</response>
    [HttpPut("{id:int}")]
    [SessionAuthorize(Role = "Organiser")]
    [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, EventRequestDto dto)
    {
        var ev = await _db.Events.Include(e => e.Organiser).Include(e => e.Venue)
            .FirstOrDefaultAsync(e => e.EventID == id);

        if (ev == null)
            return NotFound(new { message = "Event not found." });
        if (ev.OrganiserID != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only edit your own events." });

        ev.EventName = dto.EventName.Trim();
        ev.EventType = dto.EventType;
        ev.EventDate = dto.EventDate!.Value;
        ev.City = dto.City.Trim();
        ev.DistanceKM = dto.DistanceKM;
        ev.Description = dto.Description;
        ev.Status = dto.Status;

        if (dto.Venue != null)
        {
            ev.Venue ??= new Venue();
            ev.Venue.StartPoint = dto.Venue.StartPoint;
            ev.Venue.EndPoint = dto.Venue.EndPoint;
            ev.Venue.RouteDescription = dto.Venue.RouteDescription;
            ev.Venue.ElevationGainM = dto.Venue.ElevationGainM;
        }

        await _db.SaveChangesAsync();
        return Ok(EventResponseDto.From(ev));
    }

    /// <summary>Deletes an event owned by the logged-in Organiser.</summary>
    /// <remarks>An event that already has enrolments cannot be deleted. Mark it Completed instead.</remarks>
    /// <param name="id">The event's ID.</param>
    /// <response code="204">Event deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is not the owning Organiser.</response>
    /// <response code="404">The event does not exist.</response>
    /// <response code="409">The event already has enrolments.</response>
    [HttpDelete("{id:int}")]
    [SessionAuthorize(Role = "Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await _db.Events.Include(e => e.Venue).Include(e => e.Categories)
            .FirstOrDefaultAsync(e => e.EventID == id);

        if (ev == null)
            return NotFound(new { message = "Event not found." });
        if (ev.OrganiserID != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only delete your own events." });

        var categoryIds = ev.Categories.Select(c => c.CategoryID).ToList();
        if (await _db.Entries.AnyAsync(en => categoryIds.Contains(en.CategoryID)))
            return Conflict(new { message = "This event already has enrolments and cannot be deleted." });

        if (ev.Venue != null) _db.Venues.Remove(ev.Venue);
        _db.Categories.RemoveRange(ev.Categories);
        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}