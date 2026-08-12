using System.Collections.ObjectModel;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Desktop.ViewModels;

public class PurchaseLine
{
    public int MedicineId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal UnitCost { get; set; }
    public int Quantity { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal LineTotal => UnitCost * Quantity;
}

public class PurchaseViewModel
{
    private readonly IMedicineRepository _medicineRepo;
    private readonly IPurchaseRepository _purchaseRepo;
    private readonly ISupplierRepository _supplierRepo;

    public ObservableCollection<Medicine> AvailableMedicines { get; } = new();
    public ObservableCollection<Supplier> Suppliers { get; } = new();
    public ObservableCollection<PurchaseLine> Lines { get; } = new();

    public decimal Total => Lines.Sum(l => l.LineTotal);

    public PurchaseViewModel(IMedicineRepository medicineRepo, IPurchaseRepository purchaseRepo, ISupplierRepository supplierRepo)
    {
        _medicineRepo = medicineRepo;
        _purchaseRepo = purchaseRepo;
        _supplierRepo = supplierRepo;
    }

    public async Task LoadAsync()
    {
        AvailableMedicines.Clear();
        foreach (var m in await _medicineRepo.GetAllAsync())
            AvailableMedicines.Add(m);

        Suppliers.Clear();
        foreach (var s in await _supplierRepo.GetAllAsync())
            Suppliers.Add(s);
    }

    public void AddLine(Medicine medicine, int qty, decimal unitCost, string? batchNumber, DateTime? expiryDate)
    {
        if (qty <= 0) return;

        Lines.Add(new PurchaseLine
        {
            MedicineId = medicine.Id,
            Name = medicine.Name,
            UnitCost = unitCost,
            Quantity = qty,
            BatchNumber = batchNumber,
            ExpiryDate = expiryDate
        });
    }

    public async Task<int> SubmitAsync(Supplier supplier, string? invoiceNumber)
    {
        var purchase = new Purchase
        {
            SupplierId = supplier.Id,
            SupplierName = supplier.Name,
            InvoiceNumber = invoiceNumber,
            TotalAmount = Total,
            Items = Lines.Select(l => new PurchaseItem
            {
                MedicineId = l.MedicineId,
                MedicineName = l.Name,
                UnitCost = l.UnitCost,
                Quantity = l.Quantity,
                BatchNumber = l.BatchNumber,
                ExpiryDate = l.ExpiryDate
            }).ToList()
        };

        var id = await _purchaseRepo.CreatePurchaseAsync(purchase);
        Lines.Clear();
        await LoadAsync();
        return id;
    }
}
