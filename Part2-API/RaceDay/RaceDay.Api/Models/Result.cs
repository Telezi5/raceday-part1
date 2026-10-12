namespace RaceDay.Api.Models;

public class Result
{
    public int ResultID { get; set; }
    public int EntryID { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    public string Status { get; set; } = "Finished";

    public Entry Entry { get; set; } = null!;
}