using PharmacyMS.Domain.Enums;

namespace PharmacyMS.Domain.Entities;

public class PendingSalePayment
{
    public int Id { get; set; }
    public int SaleId { get; set; }
    public string CustomerName { get; set; } = "";
    public decimal Amount { get; set; }
    public string Note { get; set; } = "";
    public int SubmittedByUserId { get; set; }
    public string SubmittedByName { get; set; } = "";
    public DateTime SubmittedAt { get; set; }
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Pending;
    public string? RejectionReason { get; set; }
}
