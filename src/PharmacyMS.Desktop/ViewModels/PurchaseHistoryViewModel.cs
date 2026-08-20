using System.Collections.ObjectModel;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Desktop.ViewModels;

public class PurchaseHistoryViewModel
{
    private readonly IPurchaseRepository _purchaseRepo;

    public ObservableCollection<Purchase> Purchases { get; } = new();

    public PurchaseHistoryViewModel(IPurchaseRepository purchaseRepo)
    {
        _purchaseRepo = purchaseRepo;
    }

    public async Task LoadAsync()
    {
        Purchases.Clear();
        foreach (var p in await _purchaseRepo.GetAllAsync())
            Purchases.Add(p);
    }

    public async Task<Purchase?> LoadDetailAsync(int purchaseId)
    {
        return await _purchaseRepo.GetByIdAsync(purchaseId);
    }
}
