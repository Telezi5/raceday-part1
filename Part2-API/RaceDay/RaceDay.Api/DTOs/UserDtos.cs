using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

public class UpdateProfileDto
{
    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Phone, StringLength(20)]
    public string? ContactNumber { get; set; }
}