namespace PharmacyMS.Application.DTOs;

public class DailySalesSummaryRow
{
    public string Date { get; set; } = "";
    public int Transactions { get; set; }
    public double Revenue { get; set; }
    public double Discount { get; set; }
    public double Tax { get; set; }
    public double NetRevenue { get; set; }
}

public class PaymentMethodBreakdownRow
{
    public string Method { get; set; } = "";
    public int Count { get; set; }
    public double Total { get; set; }
}

public class TaxReportRow
{
    public string Date { get; set; } = "";
    public double Revenue { get; set; }
    public double TaxAmount { get; set; }
    public double TaxRate { get; set; }
}

public class InventoryValuationRow
{
    public string MedicineName { get; set; } = "";
    public string Category { get; set; } = "";
    public int Quantity { get; set; }
    public double CostPrice { get; set; }
    public double RetailPrice { get; set; }
    public double CostValue { get; set; }
    public double RetailValue { get; set; }
    public double PotentialProfit { get; set; }
}

public class PurchaseVsSalesRow
{
    public string MedicineName { get; set; } = "";
    public double Purchased { get; set; }
    public double Sold { get; set; }
    public double PurchaseCost { get; set; }
    public double SaleRevenue { get; set; }
    public double Profit { get; set; }
}

public class SupplierPaymentRow
{
    public string SupplierName { get; set; } = "";
    public string Phone { get; set; } = "";
    public int TotalInvoices { get; set; }
    public double TotalAmount { get; set; }
    public double AmountPaid { get; set; }
    public double Balance { get; set; }
    public string Status { get; set; } = "";
}
