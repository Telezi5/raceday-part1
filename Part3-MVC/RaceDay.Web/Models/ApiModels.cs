namespace RaceDay.Web.Models;

public class UserDto
{
    public int UserID { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public string? ContactNumber { get; set; }
    public DateTime DateRegistered { get; set; }
    public string? ProfilePictureUrl { get; set; }
}

public class VenueDto
{
    public string StartPoint { get; set; } = "";
    public string EndPoint { get; set; } = "";
    public string? RouteDescription { get; set; }
    public int? ElevationGainM { get; set; }
}

public class EventDto
{
    public int EventID { get; set; }
    public int OrganiserID { get; set; }
    public string OrganiserName { get; set; } = "";
    public string EventName { get; set; } = "";
    public string EventType { get; set; } = "";
    public DateTime EventDate { get; set; }
    public string City { get; set; } = "";
    public decimal? DistanceKM { get; set; }
    public string? Description { get; set; }
    public string? Status { get; set; }
    public VenueDto? Venue { get; set; }
    public string? BannerUrl { get; set; }
}

public class CategoryDto
{
    public int CategoryID { get; set; }
    public int EventID { get; set; }
    public string CategoryName { get; set; } = "";
    public decimal? DistanceKM { get; set; }
    public decimal? EntryFee { get; set; }
    public int? MaxParticipants { get; set; }
}

public class ResultDto
{
    public int ResultID { get; set; }
    public int EntryID { get; set; }
    public string? FinishTime { get; set; }
    public int? Position { get; set; }
    public string? Status { get; set; }
}

public class EntryDto
{
    public int EntryID { get; set; }
    public int ParticipantID { get; set; }
    public string ParticipantName { get; set; } = "";
    public int CategoryID { get; set; }
    public string CategoryName { get; set; } = "";
    public int EventID { get; set; }
    public string EventName { get; set; } = "";
    public string? BibNumber { get; set; }
    public DateTime EntryDate { get; set; }
    public string? PaymentStatus { get; set; }
    public ResultDto? Result { get; set; }
}

public class LeaderboardRowDto
{
    public int Position { get; set; }
    public string ParticipantName { get; set; } = "";
    public string? BibNumber { get; set; }
    public string? FinishTime { get; set; }
    public string? Status { get; set; }
}