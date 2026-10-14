using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.DTOs;

public class CategoryRequestDto
{
    [Required, StringLength(50)]
    public string CategoryName { get; set; } = string.Empty;

    [Range(0.01, 999.99)]
    public decimal DistanceKM { get; set; }

    [Range(0, 999999.99)]
    public decimal EntryFee { get; set; }

    [Range(1, 100000)]
    public int MaxParticipants { get; set; } = 100;
}

public class CategoryResponseDto
{
    public int CategoryID { get; set; }
    public int EventID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal DistanceKM { get; set; }
    public decimal EntryFee { get; set; }
    public int MaxParticipants { get; set; }

    public static CategoryResponseDto From(Category c) => new()
    {
        CategoryID = c.CategoryID,
        EventID = c.EventID,
        CategoryName = c.CategoryName,
        DistanceKM = c.DistanceKM,
        EntryFee = c.EntryFee,
        MaxParticipants = c.MaxParticipants
    };
}