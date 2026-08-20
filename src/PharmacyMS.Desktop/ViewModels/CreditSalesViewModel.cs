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
    public string Phone { get; set; } = "—";
    public string Email { get; set; } = "";
    public string Address { get; set; } = "";
    public decimal TotalAmount => Sale.TotalAmount;
    public decimal AmountPaid => Sale.AmountPaid;
    public decimal Balance => Sale.TotalAmount - Sale.AmountPaid;
    public string Status => Balance <= 0 ? "Paid" : AmountPaid > 0 ? "Partial" : "Unpaid";

    public CreditSaleRow(Sale sale) { Sale = sale; }
}

public class CustomerCreditSummary
{
    public string CustomerName { get; set; } = "";
    public string Phone { get; set; } = "—";
    public string Email { get; set; } = "";
    public string Address { get; set; } = "";
    public int TotalInvoices { get; set; }
    public decimal TotalOwed { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal Balance => TotalOwed - TotalPaid;
    public DateTime LastSaleDate { get; set; }
}

public class CreditSalesViewModel
{
    private readonly ISaleRepository _saleRepo;
    private readonly ICustomerRepository _customerRepo;
    private readonly IPendingSalePaymentRepository _pendingPaymentRepo;

    public ObservableCollection<CreditSaleRow> CreditSales { get; } = new();
    public ObservableCollection<CreditSaleRow> FilteredSales { get; } = new();
    public ObservableCollection<CustomerCreditSummary> CustomerSummaries { get; } = new();

    public decimal TotalOutstanding => CreditSales.Sum(r => r.Balance);
    public decimal TotalOverdue => CreditSales.Where(r => r.Balance > 0 && r.CreatedAt < DateTime.Today.AddDays(-30)).Sum(r => r.Balance);
    public int UnpaidCount => CreditSales.Count(r => r.Balance > 0);
    public int CustomersCount => CreditSales.Select(r => r.CustomerName).Distinct().Count();

    public CreditSalesViewModel(ISaleRepository saleRepo, ICustomerRepository customerRepo, IPendingSalePaymentRepository pendingPaymentRepo)
    {
        _saleRepo = saleRepo;
        _customerRepo = customerRepo;
        _pendingPaymentRepo = pendingPaymentRepo;
    }

    public async Task LoadAsync()
    {
        var sales = await _saleRepo.GetCreditSalesAsync();
        CreditSales.Clear();
        FilteredSales.Clear();
        CustomerSummaries.Clear();

        var customerCache = new Dictionary<int, Customer?>();

        foreach (var s in sales)
        {
            var row = new CreditSaleRow(s);

            if (s.CustomerId.HasValue)
            {
                if (!customerCache.TryGetValue(s.CustomerId.Value, out var cust))
                {
                    cust = await _customerRepo.GetByIdAsync(s.CustomerId.Value);
                    customerCache[s.CustomerId.Value] = cust;
                }
                if (cust != null)
                {
                    row.Phone = !string.IsNullOrWhiteSpace(cust.Phone) ? cust.Phone! : "—";
                    row.Email = cust.Email ?? "";
                    row.Address = cust.Address ?? "";
                }
            }

            CreditSales.Add(row);
            FilteredSales.Add(row);
        }

        // Build customer summaries
        var grouped = CreditSales
            .GroupBy(r => r.CustomerName)
            .Select(g => new CustomerCreditSummary
            {
                CustomerName = g.Key,
                Phone = g.FirstOrDefault(r => r.Phone != "—")?.Phone ?? "—",
                Email = g.FirstOrDefault(r => !string.IsNullOrWhiteSpace(r.Email))?.Email ?? "",
                Address = g.FirstOrDefault(r => !string.IsNullOrWhiteSpace(r.Address))?.Address ?? "",
                TotalInvoices = g.Count(),
                TotalOwed = g.Sum(r => r.TotalAmount),
                TotalPaid = g.Sum(r => r.AmountPaid),
                LastSaleDate = g.Max(r => r.CreatedAt)
            })
            .OrderByDescending(s => s.Balance);

        foreach (var s in grouped) CustomerSummaries.Add(s);
    }

    public void ApplyFilter(string query)
    {
        FilteredSales.Clear();
        var q = query?.Trim().ToLowerInvariant() ?? "";
        var results = string.IsNullOrEmpty(q)
            ? CreditSales
            : CreditSales.Where(r =>
                r.CustomerName.ToLowerInvariant().Contains(q) ||
                r.InvoiceNumber.ToLowerInvariant().Contains(q));
        foreach (var r in results) FilteredSales.Add(r);
    }

    public async Task SubmitPaymentForApprovalAsync(CreditSaleRow row, decimal amount, string note = "")
    {
        if (amount <= 0) return;
        await _pendingPaymentRepo.CreateAsync(new PharmacyMS.Domain.Entities.PendingSalePayment
        {
            SaleId = row.Sale.Id,
            CustomerName = row.CustomerName,
            Amount = amount,
            Note = note,
            SubmittedByUserId = PharmacyMS.Application.Services.SessionManager.CurrentUser?.Id ?? 0,
            SubmittedByName = PharmacyMS.Application.Services.SessionManager.CurrentUser?.FullName ?? "Unknown",
            SubmittedAt = DateTime.Now
        });
        // Do NOT call LoadAsync() — the sale balance is unchanged until approval
    }

    // Kept for use by ApprovalsViewModel when approving a pending payment
    public async Task RecordPaymentAsync(CreditSaleRow row, decimal amount, string note = "")
    {
        if (amount <= 0) return;
        await _saleRepo.RecordPaymentAsync(row.Sale.Id, amount, note);
        await LoadAsync();
    }

    public async Task<List<PharmacyMS.Domain.Entities.SalePayment>> GetPaymentHistoryAsync(int saleId)
    {
        return await _saleRepo.GetPaymentsAsync(saleId);
    }
}
