using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.DTOs;

public class VenueDto
{
    [Required, StringLength(150)]
    public string StartPoint { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string EndPoint { get; set; } = string.Empty;

    public string? RouteDescription { get; set; }

    [Range(0, 10000)]
    public int ElevationGainM { get; set; }
}

public class EventRequestDto
{
    [Required, StringLength(150)]
    public string EventName { get; set; } = string.Empty;

    [Required, RegularExpression("^(Run|Walk|Cycle)$",
        ErrorMessage = "Event type must be Run, Walk or Cycle.")]
    public string EventType { get; set; } = string.Empty;

    [Required]
    public DateOnly? EventDate { get; set; }

    [Required, StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Range(0.01, 999.99)]
    public decimal DistanceKM { get; set; }

    public string? Description { get; set; }

    [RegularExpression("^(Draft|Published|Completed)$",
        ErrorMessage = "Status must be Draft, Published or Completed.")]
    public string Status { get; set; } = "Draft";

    public VenueDto? Venue { get; set; }
}

public class EventResponseDto
{
    public int EventID { get; set; }
    public int OrganiserID { get; set; }
    public string OrganiserName { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public string City { get; set; } = string.Empty;
    public decimal DistanceKM { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public VenueDto? Venue { get; set; }

    public static EventResponseDto From(Event e) => new()
    {
        EventID = e.EventID,
        OrganiserID = e.OrganiserID,
        OrganiserName = e.Organiser?.FullName ?? string.Empty,
        EventName = e.EventName,
        EventType = e.EventType,
        EventDate = e.EventDate,
        City = e.City,
        DistanceKM = e.DistanceKM,
        Description = e.Description,
        Status = e.Status,
        Venue = e.Venue == null ? null : new VenueDto
        {
            StartPoint = e.Venue.StartPoint,
            EndPoint = e.Venue.EndPoint,
            RouteDescription = e.Venue.RouteDescription,
            ElevationGainM = e.Venue.ElevationGainM
        }
    };
}