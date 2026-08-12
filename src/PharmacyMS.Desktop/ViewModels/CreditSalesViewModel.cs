using System.Collections.ObjectModel;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Desktop.ViewModels;

public class CreditSaleRow
{
    public Sale Sale { get; }
    public string InvoiceNumber => Sale.InvoiceNumber;
    public DateTime CreatedAt => Sale.CreatedAt;
    public string CustomerName => Sale.CustomerName;
    public decimal TotalAmount => Sale.TotalAmount;
    public decimal AmountPaid => Sale.AmountPaid;
    public decimal Balance => Sale.TotalAmount - Sale.AmountPaid;

    public CreditSaleRow(Sale sale)
    {
        Sale = sale;
    }
}

public class CreditSalesViewModel
{
    private readonly ISaleRepository _saleRepo;

    public ObservableCollection<CreditSaleRow> CreditSales { get; } = new();
    public decimal TotalOutstanding => CreditSales.Sum(r => r.Balance);

    public CreditSalesViewModel(ISaleRepository saleRepo)
    {
        _saleRepo = saleRepo;
    }

    public async Task LoadAsync()
    {
        var sales = await _saleRepo.GetCreditSalesAsync();
        CreditSales.Clear();
        foreach (var s in sales) CreditSales.Add(new CreditSaleRow(s));
    }

    public async Task RecordPaymentAsync(CreditSaleRow row, decimal amount)
    {
        if (amount <= 0) return;
        await _saleRepo.RecordPaymentAsync(row.Sale.Id, amount);
        await LoadAsync();
    }
}
