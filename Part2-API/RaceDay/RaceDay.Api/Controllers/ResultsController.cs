using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api")]
public class ResultsController : ControllerBase
{
    private readonly RaceDayContext _db;

    public ResultsController(RaceDayContext db) => _db = db;

    private int? CurrentUserId => HttpContext.Session.GetInt32("UserId");

    private static bool TryValidate(ResultRequestDto dto, out TimeSpan? time, out string? error)
    {
        time = null;
        error = null;

        if (dto.Status == "Finished")
        {
            if (string.IsNullOrWhiteSpace(dto.FinishTime) || dto.Position == null)
            {
                error = "A Finished result needs both a finish time and a position.";
                return false;
            }
            time = TimeSpan.Parse(dto.FinishTime, CultureInfo.InvariantCulture);
        }
        else if (!string.IsNullOrWhiteSpace(dto.FinishTime) || dto.Position != null)
        {
            error = "A DNF or DNS result cannot have a finish time or position.";
            return false;
        }

        return true;
    }

    /// <summary>Captures the result (finish time and position) for an entry after the event.</summary>
    /// <remarks>
    /// Expects finishTime (hh:mm:ss), position and status (Finished, DNF or DNS).
    /// Finished results need a time and position. DNF and DNS results must not have either.
    /// Only the Organiser who owns the event may do this.
    /// </remarks>
    /// <param name="entryId">The entry's ID.</param>
    /// <response code="201">Result recorded.</response>
    /// <response code="400">Validation error.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is not the owning Organiser.</response>
    /// <response code="404">The entry does not exist.</response>
    /// <response code="409">A result has already been recorded for this entry.</response>
    [HttpPost("entries/{entryId:int}/result")]
    [SessionAuthorize(Role = "Organiser")]
    [ProducesResponseType(typeof(ResultResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(int entryId, ResultRequestDto dto)
    {
        var entry = await _db.Entries
            .Include(e => e.Category).ThenInclude(c => c.Event)
            .Include(e => e.Result)
            .FirstOrDefaultAsync(e => e.EntryID == entryId);

        if (entry == null)
            return NotFound(new { message = "Entry not found." });
        if (entry.Category.Event.OrganiserID != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only record results for your own events." });
        if (entry.Result != null)
            return Conflict(new { message = "A result has already been recorded for this entry." });
        if (!TryValidate(dto, out var time, out var error))
            return BadRequest(new { message = error });

        var result = new Result
        {
            EntryID = entryId,
            FinishTime = time,
            Position = dto.Position,
            Status = dto.Status
        };

        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        return Created($"/api/entries/{entryId}/result", ResultResponseDto.From(result));
    }

    /// <summary>Corrects an already-recorded result. Only the owning Organiser may do this.</summary>
    /// <param name="id">The result's ID.</param>
    /// <response code="200">Result updated.</response>
    /// <response code="400">Validation error.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is not the owning Organiser.</response>
    /// <response code="404">The result does not exist.</response>
    [HttpPut("results/{id:int}")]
    [SessionAuthorize(Role = "Organiser")]
    [ProducesResponseType(typeof(ResultResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, ResultRequestDto dto)
    {
        var result = await _db.Results
            .Include(r => r.Entry).ThenInclude(e => e.Category).ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(r => r.ResultID == id);

        if (result == null)
            return NotFound(new { message = "Result not found." });
        if (result.Entry.Category.Event.OrganiserID != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only edit results for your own events." });
        if (!TryValidate(dto, out var time, out var error))
            return BadRequest(new { message = error });

        result.FinishTime = time;
        result.Position = dto.Position;
        result.Status = dto.Status;

        await _db.SaveChangesAsync();
        return Ok(ResultResponseDto.From(result));
    }

    /// <summary>Returns the result for one entry. Visible to the entry's Participant and the event's Organiser.</summary>
    /// <param name="entryId">The entry's ID.</param>
    /// <response code="200">The result.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is neither the Participant nor the event's Organiser.</response>
    /// <response code="404">The entry does not exist, or has no result yet.</response>
    [HttpGet("entries/{entryId:int}/result")]
    [SessionAuthorize]
    [ProducesResponseType(typeof(ResultResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEntry(int entryId)
    {
        var entry = await _db.Entries
            .Include(e => e.Category).ThenInclude(c => c.Event)
            .Include(e => e.Result)
            .FirstOrDefaultAsync(e => e.EntryID == entryId);

        if (entry == null)
            return NotFound(new { message = "Entry not found." });

        var isParticipant = entry.ParticipantID == CurrentUserId;
        var isOrganiser = entry.Category.Event.OrganiserID == CurrentUserId;
        if (!isParticipant && !isOrganiser)
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only view your own results." });

        if (entry.Result == null)
            return NotFound(new { message = "No result has been recorded yet." });

        return Ok(ResultResponseDto.From(entry.Result));
    }

    /// <summary>Returns the public leaderboard for a category, ordered by finishing position.</summary>
    /// <param name="categoryId">The category's ID.</param>
    /// <response code="200">Array of results ordered by position.</response>
    /// <response code="404">The category does not exist.</response>
    [HttpGet("categories/{categoryId:int}/results")]
    [ProducesResponseType(typeof(List<LeaderboardRowDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Leaderboard(int categoryId)
    {
        var category = await _db.Categories.Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryID == categoryId);

        if (category == null ||
            (category.Event.Status == "Draft" && category.Event.OrganiserID != CurrentUserId))
            return NotFound(new { message = "Category not found." });

        var rows = await _db.Results
            .Where(r => r.Entry.CategoryID == categoryId)
            .OrderBy(r => r.Position == null)
            .ThenBy(r => r.Position)
            .Select(r => new
            {
                r.Position,
                r.Entry.Participant.FullName,
                r.Entry.BibNumber,
                r.FinishTime,
                r.Status
            })
            .ToListAsync();

        return Ok(rows.Select(r => new LeaderboardRowDto
        {
            Position = r.Position,
            ParticipantName = r.FullName,
            BibNumber = r.BibNumber,
            FinishTime = r.FinishTime?.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture),
            Status = r.Status
        }));
    }
}