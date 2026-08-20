using PharmacyMS.Domain.Enums;

namespace PharmacyMS.Domain.Entities;

public class PurchaseOrder
{
    public int Id { get; set; }
    public int? SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;
    public DateTime? ExpectedDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedByUserId { get; set; }
    public List<PurchaseOrderItem> Items { get; set; } = new();

    public decimal TotalAmount => Items.Sum(i => i.LineTotal);
}
