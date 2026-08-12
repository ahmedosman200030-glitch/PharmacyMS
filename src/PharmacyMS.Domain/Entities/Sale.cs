namespace PharmacyMS.Domain.Entities;

public class Sale
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public int CashierId { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public int? CustomerId { get; set; }
    public string CustomerName { get; set; } = "Walk-in Customer";
    public string PaymentMethod { get; set; } = "Cash";
    public decimal TotalDiscount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal ChangeDue { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<SaleItem> Items { get; set; } = new();
}
