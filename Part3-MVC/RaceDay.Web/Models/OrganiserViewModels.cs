using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

public class EventFormViewModel
{
    public int EventID { get; set; }
    [Required, Display(Name = "Event name")] public string EventName { get; set; } = "";
    [Required, Display(Name = "Event type")] public string EventType { get; set; } = "Run";
    [Required, DataType(DataType.Date), Display(Name = "Event date")] public DateTime EventDate { get; set; } = DateTime.Today.AddDays(30);
    [Required, Display(Name = "City / location")] public string City { get; set; } = "";
    [Required(ErrorMessage = "Please enter the distance in km."), Display(Name = "Distance (km)")] public decimal? DistanceKM { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "Published";
    [Required, Display(Name = "Start point")] public string StartPoint { get; set; } = "";
    [Required, Display(Name = "End point")] public string EndPoint { get; set; } = "";
    [Display(Name = "Route description")] public string? RouteDescription { get; set; }
    [Display(Name = "Elevation gain (m)")] public int? ElevationGainM { get; set; }
    public string? BannerUrl { get; set; }
    public IFormFile? Banner { get; set; }
}

public class CategoryFormViewModel
{
    [Required, Display(Name = "Category name")] public string CategoryName { get; set; } = "";
    [Display(Name = "Distance (km)")] public decimal? DistanceKM { get; set; }
    [Display(Name = "Entry fee (R)")] public decimal? EntryFee { get; set; }
    [Display(Name = "Max participants")] public int? MaxParticipants { get; set; }
}

public class EventSummary
{
    public EventDto Event { get; set; } = new();
    public int Enrolments { get; set; }
}

public class DashboardViewModel
{
    public List<EventSummary> Events { get; set; } = new();
    public int TotalEvents => Events.Count;
    public int TotalEnrolments => Events.Sum(e => e.Enrolments);
    public List<EventSummary> Upcoming => Events
        .Where(e => e.Event.EventDate.Date >= DateTime.Today)
        .OrderBy(e => e.Event.EventDate).ToList();
}

public class EventCategoriesViewModel
{
    public EventDto Event { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = new();
}

public class EventEntriesViewModel
{
    public EventDto Event { get; set; } = new();
    public List<EntryDto> Entries { get; set; } = new();
}