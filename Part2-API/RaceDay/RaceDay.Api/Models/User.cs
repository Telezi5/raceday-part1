namespace RaceDay.Api.Models;

public class User
{
    public int UserID { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "Participant";
    public string? ContactNumber { get; set; }
    public DateTime DateRegistered { get; set; } = DateTime.Now;

    public List<Event> OrganisedEvents { get; set; } = new();
    public List<Entry> Entries { get; set; } = new();
}