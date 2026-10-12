namespace RaceDay.Api.Models;

public class Venue
{
    public int VenueID { get; set; }
    public int EventID { get; set; }
    public string StartPoint { get; set; } = string.Empty;
    public string EndPoint { get; set; } = string.Empty;
    public string? RouteDescription { get; set; }
    public int ElevationGainM { get; set; }

    public Event Event { get; set; } = null!;
}