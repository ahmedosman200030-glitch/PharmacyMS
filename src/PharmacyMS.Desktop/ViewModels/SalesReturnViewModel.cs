using System.Collections.ObjectModel;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Application.Services;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Desktop.ViewModels;

public class SalesReturnViewModel
{
    private readonly ISaleRepository _saleRepo;
    private readonly ISaleReturnRepository _returnRepo;

    public ObservableCollection<Sale> RecentSales { get; } = new();
    public ObservableCollection<SaleReturn> RecentReturns { get; } = new();

    public SalesReturnViewModel(ISaleRepository saleRepo, ISaleReturnRepository returnRepo)
    {
        _saleRepo = saleRepo;
        _returnRepo = returnRepo;
    }

    public async Task LoadAsync()
    {
        RecentSales.Clear();
        var sales = await _saleRepo.GetAllAsync();
        foreach (var s in sales.OrderByDescending(x => x.CreatedAt).Take(100))
            RecentSales.Add(s);

        RecentReturns.Clear();
        var recent = await _returnRepo.GetRecentAsync(50);
        foreach (var r in recent)
            RecentReturns.Add(r);
    }

    /// <summary>
    /// Throws InvalidOperationException if quantity exceeds what was actually sold on this item
    /// (accounting for anything already returned against the same sale).
    /// </summary>
    public async Task SubmitAsync(Sale sale, SaleItem item, int quantity, decimal unitPrice, string paymentMethod, string reason)
    {
        var alreadyReturned = (await _returnRepo.GetByOriginalSaleIdAsync(sale.Id))
            .Where(r => r.MedicineId == item.MedicineId)
            .Sum(r => r.Quantity);

        var remaining = item.Quantity - alreadyReturned;
        if (quantity > remaining)
            throw new InvalidOperationException(
                $"Only {remaining} unit(s) of \"{item.MedicineName}\" remain returnable on invoice {sale.InvoiceNumber} (sold {item.Quantity}, already returned {alreadyReturned}).");

        var saleReturn = new SaleReturn
        {
            MedicineId = item.MedicineId,
            MedicineName = item.MedicineName,
            Quantity = quantity,
            UnitPrice = unitPrice,
            RefundAmount = unitPrice * quantity,
            PaymentMethod = paymentMethod,
            Reason = reason,
            OriginalSaleId = sale.Id,
            ProcessedByUserId = SessionManager.CurrentUser?.Id ?? 0,
            ProcessedByName = SessionManager.CurrentUser?.FullName ?? "Unknown"
        };

        await _returnRepo.CreateAsync(saleReturn);
        await LoadAsync();
    }
}
