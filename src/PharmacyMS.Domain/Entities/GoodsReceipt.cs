using PharmacyMS.Domain.Enums;

namespace PharmacyMS.Domain.Entities;

public class GoodsReceipt
{
    public int Id { get; set; }
    public int PurchaseOrderId { get; set; }
    public DateTime ReceivedAt { get; set; }
    public int ReceivedByUserId { get; set; }
    public string? Notes { get; set; }
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Pending;
    public string? RejectionReason { get; set; }
    public string? ReceivedByUserName { get; set; }
    public List<GoodsReceiptItem> Items { get; set; } = new();

    public decimal TotalAmount => Items.Sum(i => i.LineTotal);
}
