namespace PharmacyMS.Domain.Entities;

public class Medicine
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string? GenericName { get; set; }
    public string? Category { get; set; }
    public string? Manufacturer { get; set; }
    public string? Supplier { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal CostPrice { get; set; }
    public int QuantityInStock { get; set; }
    public int ReorderLevel { get; set; } = 10;
    public DateTime? ExpiryDate { get; set; }
    public string? BatchNumber { get; set; }
    public string Unit { get; set; } = "Box";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
