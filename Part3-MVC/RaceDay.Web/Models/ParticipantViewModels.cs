using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

public class HomeViewModel
{
    public List<EventDto> Events { get; set; } = new();
    public string? City { get; set; }
    public DateTime? Date { get; set; }
    public string? Type { get; set; }
}

public class EventDetailsViewModel
{
    public EventDto Event { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = new();
}

public class MyEntryRow
{
    public EntryDto Entry { get; set; } = new();
    public EventDto? Event { get; set; }
}

public class MyResultRow
{
    public EntryDto Entry { get; set; } = new();
    public EventDto? Event { get; set; }
    public int? TotalFinishers { get; set; }

    public string PositionText
    {
        get
        {
            var p = Entry.Result?.Position;
            if (p == null) return "-";
            var suffix = (p % 100 is >= 11 and <= 13) ? "th" : (p % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };
            var text = p + suffix;
            return TotalFinishers.HasValue && TotalFinishers > 0 ? $"{text} of {TotalFinishers} finishers" : text;
        }
    }
}

public class ProfileViewModel
{
    [Required, Display(Name = "Full name")] public string FullName { get; set; } = "";
    [Display(Name = "Contact number")] public string? ContactNumber { get; set; }
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public DateTime DateRegistered { get; set; }
    public string? ProfilePictureUrl { get; set; }
}