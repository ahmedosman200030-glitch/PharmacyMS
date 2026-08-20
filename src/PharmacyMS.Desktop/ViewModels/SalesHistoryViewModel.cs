using System.Collections.ObjectModel;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Desktop.ViewModels;

public class SalesHistoryViewModel
{
    private readonly ISaleRepository _saleRepo;
    private readonly ICustomerRepository? _customerRepo;

    public ObservableCollection<Sale> Sales { get; } = new();

    public SalesHistoryViewModel(ISaleRepository saleRepo, ICustomerRepository? customerRepo = null)
    {
        _saleRepo = saleRepo;
        _customerRepo = customerRepo;
    }

    public async Task<Customer?> GetCustomerAsync(int customerId)
    {
        if (_customerRepo == null) return null;
        return await _customerRepo.GetByIdAsync(customerId);
    }

    public async Task LoadAllAsync()
    {
        var sales = await _saleRepo.GetAllAsync();
        Sales.Clear();
        foreach (var s in sales) Sales.Add(s);
    }

    public async Task SearchByInvoiceAsync(string invoiceNumber)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber))
        {
            await LoadAllAsync();
            return;
        }

        var sale = await _saleRepo.GetByInvoiceAsync(invoiceNumber);
        Sales.Clear();
        if (sale != null) Sales.Add(sale);
    }

    public async Task FilterByDateRangeAsync(DateTime from, DateTime to)
    {
        var sales = await _saleRepo.GetByDateRangeAsync(from, to);
        Sales.Clear();
        foreach (var s in sales) Sales.Add(s);
    }
}
