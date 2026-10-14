using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.DTOs;

public class EntryRequestDto
{
    [RegularExpression("^(Pending|Paid|Refunded)$",
        ErrorMessage = "Payment status must be Pending, Paid or Refunded.")]
    public string PaymentStatus { get; set; } = "Pending";
}

public class EntryResponseDto
{
    public int EntryID { get; set; }
    public int ParticipantID { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public int CategoryID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int EventID { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string BibNumber { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public ResultResponseDto? Result { get; set; }

    public static EntryResponseDto From(Entry e) => new()
    {
        EntryID = e.EntryID,
        ParticipantID = e.ParticipantID,
        ParticipantName = e.Participant?.FullName ?? string.Empty,
        CategoryID = e.CategoryID,
        CategoryName = e.Category?.CategoryName ?? string.Empty,
        EventID = e.Category?.EventID ?? 0,
        EventName = e.Category?.Event?.EventName ?? string.Empty,
        BibNumber = e.BibNumber,
        EntryDate = e.EntryDate,
        PaymentStatus = e.PaymentStatus,
        Result = e.Result == null ? null : ResultResponseDto.From(e.Result)
    };
}