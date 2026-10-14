using System.ComponentModel.DataAnnotations;
using System.Globalization;
using RaceDay.Api.Models;

namespace RaceDay.Api.DTOs;

public class ResultRequestDto
{
    /// <summary>Finish time as hh:mm:ss, for example 00:52:31.</summary>
    [RegularExpression(@"^([01]?\d|2[0-3]):[0-5]\d:[0-5]\d$",
        ErrorMessage = "Finish time must be in hh:mm:ss format.")]
    public string? FinishTime { get; set; }

    [Range(1, 100000)]
    public int? Position { get; set; }

    [Required, RegularExpression("^(Finished|DNF|DNS)$",
        ErrorMessage = "Status must be Finished, DNF or DNS.")]
    public string Status { get; set; } = "Finished";
}

public class ResultResponseDto
{
    public int ResultID { get; set; }
    public int EntryID { get; set; }
    public string? FinishTime { get; set; }
    public int? Position { get; set; }
    public string Status { get; set; } = string.Empty;

    public static ResultResponseDto From(Result r) => new()
    {
        ResultID = r.ResultID,
        EntryID = r.EntryID,
        FinishTime = r.FinishTime?.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture),
        Position = r.Position,
        Status = r.Status
    };
}

public class LeaderboardRowDto
{
    public int? Position { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string BibNumber { get; set; } = string.Empty;
    public string? FinishTime { get; set; }
    public string Status { get; set; } = string.Empty;
}