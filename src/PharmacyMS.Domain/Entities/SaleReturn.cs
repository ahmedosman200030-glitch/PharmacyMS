namespace PharmacyMS.Domain.Entities;

public class SaleReturn
{
    public int Id { get; set; }
    public int MedicineId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal RefundAmount { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public string Reason { get; set; } = string.Empty;
    public int? OriginalSaleId { get; set; }
    public int ProcessedByUserId { get; set; }
    public string ProcessedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
