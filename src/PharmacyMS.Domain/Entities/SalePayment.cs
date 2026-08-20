namespace PharmacyMS.Domain.Entities;

public class SalePayment
{
    public int Id { get; set; }
    public int SaleId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaidAt { get; set; }
    public string Note { get; set; } = "";
}
