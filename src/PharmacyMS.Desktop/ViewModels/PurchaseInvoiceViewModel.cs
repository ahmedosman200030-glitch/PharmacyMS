using System.Collections.ObjectModel;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Desktop.ViewModels;

public class PurchaseInvoiceViewModel
{
    private readonly IPurchaseRepository _purchaseRepo;
    private List<Purchase> _all = new();

    public ObservableCollection<Purchase> Purchases { get; } = new();
    public Purchase? Selected { get; private set; }

    public int TotalInvoices => _all.Count;
    public decimal TotalAmountSum => _all.Sum(p => p.TotalAmount);
    public decimal PaidAmountSum => _all.Sum(p => p.AmountPaid);
    public decimal DueAmountSum => _all.Sum(p => p.DueAmount);

    public PurchaseInvoiceViewModel(IPurchaseRepository purchaseRepo)
    {
        _purchaseRepo = purchaseRepo;
    }

    public async Task LoadAsync()
    {
        _all = (await _purchaseRepo.GetAllAsync()).ToList();
        ApplyFilter(null);
    }

    public void ApplyFilter(string? searchText)
    {
        Purchases.Clear();
        var query = string.IsNullOrWhiteSpace(searchText)
            ? _all
            : _all.Where(p =>
                p.SupplierName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                (p.InvoiceNumber ?? string.Empty).Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                p.Id.ToString().Contains(searchText));

        foreach (var p in query)
            Purchases.Add(p);
    }

    public async Task<Purchase?> LoadDetailAsync(int purchaseId)
    {
        Selected = await _purchaseRepo.GetByIdAsync(purchaseId);
        return Selected;
    }

    public async Task RecordPaymentAsync(int purchaseId, decimal amount)
    {
        await _purchaseRepo.RecordPaymentAsync(purchaseId, amount);
        await LoadAsync();
    }

    public async Task ApproveAsync(Purchase purchase)
    {
        purchase.ApprovalStatus = PharmacyMS.Domain.Enums.ApprovalStatus.Approved;
        await _purchaseRepo.UpdateApprovalStatusAsync(purchase.Id, purchase.ApprovalStatus);
        await LoadAsync();
    }

    public async Task RejectAsync(Purchase purchase)
    {
        purchase.ApprovalStatus = PharmacyMS.Domain.Enums.ApprovalStatus.Rejected;
        await _purchaseRepo.UpdateApprovalStatusAsync(purchase.Id, purchase.ApprovalStatus);
        await LoadAsync();
    }
}
