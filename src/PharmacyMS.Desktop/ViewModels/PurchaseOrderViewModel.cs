using System.Collections.ObjectModel;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Application.Interfaces.Services;
using PharmacyMS.Domain.Entities;
using PharmacyMS.Domain.Enums;

namespace PharmacyMS.Desktop.ViewModels;

public class PurchaseOrderLine
{
    public int MedicineId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal UnitCost { get; set; }
    public int Quantity { get; set; }
    public string Unit { get; set; } = "Box";
    public decimal LineTotal => UnitCost * Quantity;
}

public class PurchaseOrderViewModel
{
    private readonly IMedicineRepository _medicineRepo;
    private readonly IPurchaseOrderRepository _orderRepo;
    private readonly ISupplierRepository _supplierRepo;
    private readonly IPurchaseOrderPdfService _pdfService;

    public ObservableCollection<Medicine> AvailableMedicines { get; } = new();
    public ObservableCollection<Supplier> Suppliers { get; } = new();
    public ObservableCollection<PurchaseOrderLine> Lines { get; } = new();

    public decimal Total => Lines.Sum(l => l.LineTotal);

    public PurchaseOrderViewModel(IMedicineRepository medicineRepo, IPurchaseOrderRepository orderRepo, ISupplierRepository supplierRepo, IPurchaseOrderPdfService pdfService)
    {
        _medicineRepo = medicineRepo;
        _orderRepo = orderRepo;
        _supplierRepo = supplierRepo;
        _pdfService = pdfService;
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

    public void AddLine(Medicine medicine, int qty, decimal unitCost, string unit)
    {
        if (qty <= 0) return;

        Lines.Add(new PurchaseOrderLine
        {
            MedicineId = medicine.Id,
            Name = medicine.Name,
            UnitCost = unitCost,
            Quantity = qty,
            Unit = unit
        });
    }

    public void RemoveLine(PurchaseOrderLine line)
    {
        Lines.Remove(line);
    }

    public PurchaseOrder? LastSubmittedOrder { get; private set; }

    public async Task<int> SubmitAsync(Supplier supplier, DateTime? expectedDate, string? notes, bool sendNow)
    {
        var order = new PurchaseOrder
        {
            SupplierId = supplier.Id,
            SupplierName = supplier.Name,
            OrderNumber = $"PO-{DateTime.Now:yyyyMMdd-HHmmss}",
            Status = sendNow ? PurchaseOrderStatus.Sent : PurchaseOrderStatus.Draft,
            ExpectedDate = expectedDate,
            Notes = notes,
            CreatedAt = DateTime.Now,
            CreatedByUserId = PharmacyMS.Application.Services.SessionManager.CurrentUser?.Id ?? 0,
            Items = Lines.Select(l => new PurchaseOrderItem
            {
                MedicineId = l.MedicineId,
                MedicineName = l.Name,
                UnitCost = l.UnitCost,
                Quantity = l.Quantity,
                Unit = l.Unit
            }).ToList()
        };

        var id = await _orderRepo.CreateAsync(order);
        order.Id = id;
        LastSubmittedOrder = order;

        Lines.Clear();
        await LoadAsync();
        return id;
    }

    public Task<string> GeneratePdfAsync(PurchaseOrder order, Supplier supplier)
        => _pdfService.GeneratePdfAsync(order, supplier);
}
