namespace PharmacyMS.Domain.Entities;

public class PurchasePayment
{
    public int Id { get; set; }
    public int PurchaseId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaidAt { get; set; }
    public string Note { get; set; } = "";
}
