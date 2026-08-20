namespace PharmacyMS.Domain.Entities;

public class PurchaseOrderItem
{
    public int Id { get; set; }
    public int PurchaseOrderId { get; set; }
    public int MedicineId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Unit { get; set; } = "Box";
    public decimal UnitCost { get; set; }

    // Cumulative quantity received so far across all GoodsReceipts for this line
    public int ReceivedQuantity { get; set; }

    public decimal LineTotal => UnitCost * Quantity;
    public int RemainingQuantity => Quantity - ReceivedQuantity;
}
