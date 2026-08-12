namespace PharmacyMS.Domain.Entities;

public class StockAdjustment
{
    public int Id { get; set; }
    public int MedicineId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public int QuantityChange { get; set; } // positive = add, negative = remove
    public string Reason { get; set; } = string.Empty;
    public int AdjustedByUserId { get; set; }
    public string AdjustedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
