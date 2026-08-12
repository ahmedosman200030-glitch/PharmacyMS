namespace PharmacyMS.Domain.Entities;

public class DailyClosing
{
    public int Id { get; set; }
    public DateTime ClosingDate { get; set; }
    public decimal CashSales { get; set; }
    public decimal CardSales { get; set; }
    public decimal MobileSales { get; set; }
    public decimal InsuranceSales { get; set; }
    public decimal ExpectedCash { get; set; }
    public decimal ActualCash { get; set; }
    public decimal Difference { get; set; }
    public string? Notes { get; set; }
    public int ClosedByUserId { get; set; }
    public string ClosedByUserName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
