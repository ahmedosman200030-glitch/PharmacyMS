using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Application.Interfaces.Services;

public class ReceiptData
{
    public string PharmacyName { get; set; } = "PharmacyMS";
    public string? LogoPath { get; set; }
    public string Address { get; set; } = "";
    public string Phone { get; set; } = "";
    public string InvoiceNumber { get; set; } = "";
    public DateTime DateTime { get; set; }
    public string CashierName { get; set; } = "";
    public string CustomerName { get; set; } = "Walk-in Customer";
    public List<ReceiptLine> Items { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public decimal AmountReceived { get; set; }
    public decimal Change { get; set; }
    public string Footer { get; set; } = "Thank you for your purchase!";
}

public class ReceiptLine
{
    public string Name { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal LineTotal { get; set; }
}

public interface IReceiptService
{
    Task<ReceiptData> BuildReceiptAsync(Sale sale, string customerName, string paymentMethod,
        decimal amountReceived, decimal change, decimal totalDiscount);
    Task PrintAsync(ReceiptData receipt);
    Task<string> SaveAsPdfAsync(ReceiptData receipt);
}
