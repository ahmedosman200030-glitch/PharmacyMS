namespace PharmacyMS.Application.DTOs;

public class TopSellingMedicine
{
    public string MedicineName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}

public class MonthlyStockReconciliationRow
{
    public string MedicineName { get; set; } = string.Empty;
    public int OpeningStock { get; set; }
    public int Received { get; set; }
    public int Dispensed { get; set; }
    public int Adjustments { get; set; }
    public int ClosingStock { get; set; }
}
