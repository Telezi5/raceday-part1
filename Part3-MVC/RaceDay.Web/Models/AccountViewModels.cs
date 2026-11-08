using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

public class LoginViewModel
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, DataType(DataType.Password)] public string Password { get; set; } = "";
}

public class RegisterViewModel
{
    [Required, Display(Name = "Full name")] public string FullName { get; set; } = "";
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, DataType(DataType.Password)] public string Password { get; set; } = "";
    [Required] public string Role { get; set; } = "Participant";
    [Display(Name = "Contact number")] public string? ContactNumber { get; set; }
    [Display(Name = "Organiser key")] public string? OrganiserKey { get; set; }
}