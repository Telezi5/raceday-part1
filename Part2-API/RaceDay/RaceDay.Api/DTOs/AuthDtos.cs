using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.DTOs;

public class RegisterDto
{
    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [Required, RegularExpression("^(Organiser|Participant)$",
        ErrorMessage = "Role must be Organiser or Participant.")]
    public string Role { get; set; } = string.Empty;

    [Phone, StringLength(20)]
    public string? ContactNumber { get; set; }

    /// <summary>Required only when registering as an Organiser.</summary>
    public string? OrganiserKey { get; set; }
}

public class LoginDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class UserDto
{
    public int UserID { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? ContactNumber { get; set; }
    public DateTime DateRegistered { get; set; }

    public static UserDto From(User u) => new()
    {
        UserID = u.UserID,
        FullName = u.FullName,
        Email = u.Email,
        Role = u.Role,
        ContactNumber = u.ContactNumber,
        DateRegistered = u.DateRegistered
    };
}