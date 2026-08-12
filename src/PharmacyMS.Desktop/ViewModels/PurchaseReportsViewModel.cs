using System.Collections.ObjectModel;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Desktop.ViewModels;

public class PurchaseReportsViewModel
{
    private readonly IPurchaseRepository _purchaseRepo;

    public ObservableCollection<SupplierSpend> BySupplier { get; } = new();
    public ObservableCollection<MedicineSpend> ByMedicine { get; } = new();
    public decimal TotalSpend { get; private set; }

    public PurchaseReportsViewModel(IPurchaseRepository purchaseRepo)
    {
        _purchaseRepo = purchaseRepo;
    }

    public async Task LoadAsync()
    {
        TotalSpend = await _purchaseRepo.GetTotalSpendAsync();

        BySupplier.Clear();
        foreach (var s in await _purchaseRepo.GetSpendBySupplierAsync())
            BySupplier.Add(s);

        ByMedicine.Clear();
        foreach (var m in await _purchaseRepo.GetSpendByMedicineAsync())
            ByMedicine.Add(m);
    }
}
