namespace PharmacyMS.Domain.Entities;

public class GoodsReceiptItem
{
    public int Id { get; set; }
    public int GoodsReceiptId { get; set; }
    public int PurchaseOrderItemId { get; set; }
    public int MedicineId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public int OrderedQuantity { get; set; }
    public int ReceivedQuantity { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal UnitCost { get; set; }

    public decimal LineTotal => UnitCost * ReceivedQuantity;
    public bool HasDiscrepancy => ReceivedQuantity != OrderedQuantity;
}
