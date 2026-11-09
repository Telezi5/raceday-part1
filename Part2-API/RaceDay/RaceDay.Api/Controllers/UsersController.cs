using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Filters;
using RaceDay.Api.Services;


namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/users/me")]
[SessionAuthorize]
public class UsersController : ControllerBase
{
    private readonly RaceDayContext _db;

    public UsersController(RaceDayContext db) => _db = db;

    private int CurrentUserId => HttpContext.Session.GetInt32("UserId")!.Value;

    /// <summary>Returns the profile of the logged-in user (either role).</summary>
    /// <response code="200">The user's profile.</response>
    /// <response code="401">Not logged in.</response>
    [HttpGet]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfile()
    {
        var user = await _db.Users.FindAsync(CurrentUserId);
        if (user == null)
        {
            HttpContext.Session.Clear();
            return Unauthorized(new { message = "Please log in first." });
        }

        return Ok(UserDto.From(user));
    }

    /// <summary>Updates the logged-in user's own name and contact number.</summary>
    /// <remarks>Expects fullName and, optionally, contactNumber. Email, role and password cannot be changed here.</remarks>
    /// <response code="200">Profile updated.</response>
    /// <response code="400">Invalid fields.</response>
    /// <response code="401">Not logged in.</response>
    [HttpPut]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfile(UpdateProfileDto dto)
    {
        var user = await _db.Users.FindAsync(CurrentUserId);
        if (user == null)
        {
            HttpContext.Session.Clear();
            return Unauthorized(new { message = "Please log in first." });
        }

        user.FullName = dto.FullName.Trim();
        user.ContactNumber = dto.ContactNumber;
        await _db.SaveChangesAsync();

        return Ok(UserDto.From(user));
    }

    /// <summary>Returns the logged-in Participant's entry and performance history, with event, category and result details.</summary>
    /// <response code="200">Array of the Participant's entries.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is not a Participant.</response>
    [HttpGet("entries")]
    [SessionAuthorize(Role = "Participant")]
    [ProducesResponseType(typeof(List<EntryResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyEntries()
    {
        var userId = CurrentUserId;

        var entries = await _db.Entries
            .Include(e => e.Participant)
            .Include(e => e.Category).ThenInclude(c => c.Event)
            .Include(e => e.Result)
            .Where(e => e.ParticipantID == userId)
            .OrderBy(e => e.Category.Event.EventDate)
            .ToListAsync();

        return Ok(entries.Select(EntryResponseDto.From));
    }

    /// <summary>Uploads or replaces the logged-in user's profile picture in Blob Storage.</summary>
    /// <remarks>Send the image as multipart/form-data in a field called "file". JPG, PNG, WEBP or GIF, up to 5 MB.</remarks>
    /// <response code="200">Picture saved. Returns the updated profile with its profilePictureUrl.</response>
    /// <response code="400">Not a valid image.</response>
    /// <response code="401">Not logged in.</response>
    [HttpPost("picture")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UploadPicture(IFormFile file, [FromServices] BlobImageService images)
    {
        var user = await _db.Users.FindAsync(CurrentUserId);
        if (user == null)
        {
            HttpContext.Session.Clear();
            return Unauthorized(new { message = "Please log in first." });
        }

        var error = BlobImageService.Validate(file);
        if (error != null)
            return BadRequest(new { message = error });

        user.ProfilePictureUrl = await images.UploadAsync(file, "profiles");
        await _db.SaveChangesAsync();

        return Ok(UserDto.From(user));
    }
}