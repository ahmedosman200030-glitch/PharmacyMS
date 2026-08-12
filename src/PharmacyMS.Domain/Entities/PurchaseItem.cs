namespace PharmacyMS.Domain.Entities;

public class PurchaseItem
{
    public int Id { get; set; }
    public int PurchaseId { get; set; }
    public int MedicineId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public decimal UnitCost { get; set; }
    public int Quantity { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal LineTotal => UnitCost * Quantity;
}
