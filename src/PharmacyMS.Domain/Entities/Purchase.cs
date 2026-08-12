namespace PharmacyMS.Domain.Entities;

public class Purchase
{
    public int Id { get; set; }
    public int? SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string? InvoiceNumber { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<PurchaseItem> Items { get; set; } = new();

    public decimal DueAmount => TotalAmount - AmountPaid;

    public string Status =>
        AmountPaid <= 0 ? "Unpaid" :
        DueAmount <= 0 ? "Paid" : "Partial";
}
