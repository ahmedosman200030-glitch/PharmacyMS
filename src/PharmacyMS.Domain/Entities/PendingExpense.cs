using PharmacyMS.Domain.Enums;

namespace PharmacyMS.Domain.Entities;

public class PendingExpense
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string Category { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public int SubmittedByUserId { get; set; }
    public string SubmittedByName { get; set; } = "";
    public DateTime SubmittedAt { get; set; }
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Pending;
    public string? RejectionReason { get; set; }
}
