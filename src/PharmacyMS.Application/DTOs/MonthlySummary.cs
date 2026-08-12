namespace PharmacyMS.Application.DTOs;

public class MonthlySummary
{
    public string MonthLabel { get; set; } = string.Empty;
    public decimal SalesTotal { get; set; }
    public decimal PurchaseTotal { get; set; }
}
