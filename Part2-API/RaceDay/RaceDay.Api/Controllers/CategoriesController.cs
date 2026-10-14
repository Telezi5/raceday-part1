using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api")]
public class CategoriesController : ControllerBase
{
    private readonly RaceDayContext _db;

    public CategoriesController(RaceDayContext db) => _db = db;

    private int? CurrentUserId => HttpContext.Session.GetInt32("UserId");

    /// <summary>Lists all race categories (for example 5km or 21km) belonging to an event.</summary>
    /// <param name="eventId">The event's ID.</param>
    /// <response code="200">Array of categories, ordered by distance.</response>
    /// <response code="404">The event does not exist (or is a draft owned by someone else).</response>
    [HttpGet("events/{eventId:int}/categories")]
    [ProducesResponseType(typeof(List<CategoryResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventID == eventId);
        if (ev == null || (ev.Status == "Draft" && ev.OrganiserID != CurrentUserId))
            return NotFound(new { message = "Event not found." });

        var categories = await _db.Categories
            .Where(c => c.EventID == eventId)
            .OrderBy(c => c.DistanceKM)
            .ToListAsync();

        return Ok(categories.Select(CategoryResponseDto.From));
    }

    /// <summary>Adds a new category to an event owned by the logged-in Organiser.</summary>
    /// <remarks>Expects categoryName, distanceKM, entryFee and maxParticipants.</remarks>
    /// <param name="eventId">The event's ID.</param>
    /// <response code="201">Category created.</response>
    /// <response code="400">Validation error.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is not the owning Organiser.</response>
    /// <response code="404">The event does not exist.</response>
    [HttpPost("events/{eventId:int}/categories")]
    [SessionAuthorize(Role = "Organiser")]
    [ProducesResponseType(typeof(CategoryResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(int eventId, CategoryRequestDto dto)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventID == eventId);
        if (ev == null)
            return NotFound(new { message = "Event not found." });
        if (ev.OrganiserID != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only add categories to your own events." });

        var category = new Category
        {
            EventID = eventId,
            CategoryName = dto.CategoryName.Trim(),
            DistanceKM = dto.DistanceKM,
            EntryFee = dto.EntryFee,
            MaxParticipants = dto.MaxParticipants
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return Created($"/api/events/{eventId}/categories", CategoryResponseDto.From(category));
    }

    /// <summary>Updates a category. Only the Organiser who owns the event may do this.</summary>
    /// <remarks>The maximum number of participants cannot be set below the current number of enrolments.</remarks>
    /// <param name="id">The category's ID.</param>
    /// <response code="200">Category updated.</response>
    /// <response code="400">Validation error.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is not the owning Organiser.</response>
    /// <response code="404">The category does not exist.</response>
    [HttpPut("categories/{id:int}")]
    [SessionAuthorize(Role = "Organiser")]
    [ProducesResponseType(typeof(CategoryResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, CategoryRequestDto dto)
    {
        var category = await _db.Categories.Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryID == id);

        if (category == null)
            return NotFound(new { message = "Category not found." });
        if (category.Event.OrganiserID != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only edit categories of your own events." });

        var enrolled = await _db.Entries.CountAsync(en => en.CategoryID == id);
        if (dto.MaxParticipants < enrolled)
            return BadRequest(new { message = $"Maximum participants cannot be below the {enrolled} existing enrolments." });

        category.CategoryName = dto.CategoryName.Trim();
        category.DistanceKM = dto.DistanceKM;
        category.EntryFee = dto.EntryFee;
        category.MaxParticipants = dto.MaxParticipants;

        await _db.SaveChangesAsync();
        return Ok(CategoryResponseDto.From(category));
    }

    /// <summary>Removes a category from an event. Only the owning Organiser may do this.</summary>
    /// <remarks>A category that already has enrolments cannot be deleted.</remarks>
    /// <param name="id">The category's ID.</param>
    /// <response code="204">Category deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is not the owning Organiser.</response>
    /// <response code="404">The category does not exist.</response>
    /// <response code="409">The category already has enrolments.</response>
    [HttpDelete("categories/{id:int}")]
    [SessionAuthorize(Role = "Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.Categories.Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryID == id);

        if (category == null)
            return NotFound(new { message = "Category not found." });
        if (category.Event.OrganiserID != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only delete categories of your own events." });

        if (await _db.Entries.AnyAsync(en => en.CategoryID == id))
            return Conflict(new { message = "This category already has enrolments and cannot be deleted." });

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}