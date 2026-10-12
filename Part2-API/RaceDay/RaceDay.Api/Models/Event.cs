namespace RaceDay.Api.Models;

public class Event
{
    public int EventID { get; set; }
    public int OrganiserID { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string EventType { get; set; } = "Run";      // Run, Walk or Cycle
    public DateOnly EventDate { get; set; }
    public string City { get; set; } = string.Empty;     // the event's location
    public decimal DistanceKM { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "Draft";

    public User Organiser { get; set; } = null!;
    public Venue? Venue { get; set; }
    public List<Category> Categories { get; set; } = new();
}