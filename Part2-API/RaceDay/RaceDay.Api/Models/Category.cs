namespace RaceDay.Api.Models;

public class Category
{
    public int CategoryID { get; set; }
    public int EventID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal DistanceKM { get; set; }
    public decimal EntryFee { get; set; }
    public int MaxParticipants { get; set; } = 100;

    public Event Event { get; set; } = null!;
    public List<Entry> Entries { get; set; } = new();
}