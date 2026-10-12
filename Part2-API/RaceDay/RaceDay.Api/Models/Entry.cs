namespace RaceDay.Api.Models;

public class Entry
{
    public int EntryID { get; set; }
    public int ParticipantID { get; set; }
    public int CategoryID { get; set; }
    public string BibNumber { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; } = DateTime.Now;
    public string PaymentStatus { get; set; } = "Pending";

    public User Participant { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public Result? Result { get; set; }
}