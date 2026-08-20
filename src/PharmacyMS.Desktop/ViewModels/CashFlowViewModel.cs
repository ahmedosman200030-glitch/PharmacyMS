using PharmacyMS.Application.Interfaces.Repositories;

namespace PharmacyMS.Desktop.ViewModels;

public class CashFlowLedgerRow
{
    public DateTime Date { get; set; }
    public string Direction { get; set; } = string.Empty; // "In" or "Out"
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class CashFlowTrendPoint
{
    public string Label { get; set; } = string.Empty;
    public decimal CashIn { get; set; }
    public decimal CashOut { get; set; }
}

public class CashFlowViewModel
{
    private readonly ISaleRepository _saleRepo;
    private readonly IPurchaseRepository _purchaseRepo;
    private readonly IExpenseRepository _expenseRepo;

    private static readonly DateTime EpochFloor = new(2000, 1, 1);

    public DateTime FromDate { get; set; } = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
    public DateTime ToDate { get; set; } = DateTime.Today;

    public decimal TotalCashIn { get; private set; }
    public decimal TotalCashOut { get; private set; }
    public decimal NetCashFlow => TotalCashIn - TotalCashOut;
    public decimal CashBalance { get; private set; }

    public List<CashFlowLedgerRow> AllRows { get; private set; } = new();
    public List<CashFlowTrendPoint> TrendPoints { get; private set; } = new();

    public string ActiveFilter { get; set; } = "All"; // All | In | Out
    public string SearchText { get; set; } = string.Empty;
    public int PageSize { get; } = 8;
    public int CurrentPage { get; set; } = 1;

    public CashFlowViewModel(ISaleRepository saleRepo, IPurchaseRepository purchaseRepo, IExpenseRepository expenseRepo)
    {
        _saleRepo = saleRepo;
        _purchaseRepo = purchaseRepo;
        _expenseRepo = expenseRepo;
    }

    public async Task LoadAsync()
    {
        var rows = await ComputeRowsAsync(FromDate, ToDate);
        AllRows = rows.OrderByDescending(r => r.Date).ToList();

        TotalCashIn = rows.Where(r => r.Direction == "In").Sum(r => r.Amount);
        TotalCashOut = rows.Where(r => r.Direction == "Out").Sum(r => r.Amount);

        var allTimeRows = await ComputeRowsAsync(EpochFloor, ToDate);
        var allTimeIn = allTimeRows.Where(r => r.Direction == "In").Sum(r => r.Amount);
        var allTimeOut = allTimeRows.Where(r => r.Direction == "Out").Sum(r => r.Amount);
        CashBalance = allTimeIn - allTimeOut;

        TrendPoints = new List<CashFlowTrendPoint>();
        var days = (ToDate.Date - FromDate.Date).Days;
        if (days is >= 0 and <= 62)
        {
            for (var d = FromDate.Date; d <= ToDate.Date; d = d.AddDays(1))
            {
                var dayIn = rows.Where(r => r.Direction == "In" && r.Date.Date == d).Sum(r => r.Amount);
                var dayOut = rows.Where(r => r.Direction == "Out" && r.Date.Date == d).Sum(r => r.Amount);
                TrendPoints.Add(new CashFlowTrendPoint { Label = d.ToString("MMM d"), CashIn = dayIn, CashOut = dayOut });
            }
        }
    }

    private async Task<List<CashFlowLedgerRow>> ComputeRowsAsync(DateTime from, DateTime to)
    {
        var rows = new List<CashFlowLedgerRow>();

        var sales = await _saleRepo.GetByDateRangeAsync(from, to);
        foreach (var s in sales)
        {
            if (s.AmountPaid > 0)
            {
                rows.Add(new CashFlowLedgerRow
                {
                    Date = s.CreatedAt, Direction = "In", Type = "Sale",
                    Description = $"Sale to {s.CustomerName}", Reference = s.InvoiceNumber, Amount = s.AmountPaid
                });
            }
        }

        var creditSales = await _saleRepo.GetCreditSalesAsync();
        foreach (var cs in creditSales)
        {
            var payments = await _saleRepo.GetPaymentsAsync(cs.Id);
            foreach (var p in payments.Where(p => p.PaidAt >= from && p.PaidAt <= to.AddDays(1)))
            {
                rows.Add(new CashFlowLedgerRow
                {
                    Date = p.PaidAt, Direction = "In", Type = "Customer Payment",
                    Description = $"Payment from {cs.CustomerName}", Reference = cs.InvoiceNumber, Amount = p.Amount
                });
            }
        }

        var purchases = (await _purchaseRepo.GetAllAsync())
            .Where(p => p.CreatedAt.Date >= from.Date && p.CreatedAt.Date <= to.Date).ToList();
        foreach (var p in purchases)
        {
            if (p.AmountPaid > 0)
            {
                rows.Add(new CashFlowLedgerRow
                {
                    Date = p.CreatedAt, Direction = "Out", Type = "Purchase",
                    Description = $"Purchase from {p.SupplierName}", Reference = p.InvoiceNumber ?? "", Amount = p.AmountPaid
                });
            }
        }

        var allPurchases = await _purchaseRepo.GetAllAsync();
        foreach (var p in allPurchases)
        {
            var payments = await _purchaseRepo.GetPaymentsAsync(p.Id);
            foreach (var pay in payments.Where(pay => pay.PaidAt >= from && pay.PaidAt <= to.AddDays(1)))
            {
                rows.Add(new CashFlowLedgerRow
                {
                    Date = pay.PaidAt, Direction = "Out", Type = "Supplier Payment",
                    Description = $"Payment to {p.SupplierName}", Reference = p.InvoiceNumber ?? "", Amount = pay.Amount
                });
            }
        }

        var expenses = await _expenseRepo.GetByDateRangeAsync(from, to);
        foreach (var e in expenses)
        {
            rows.Add(new CashFlowLedgerRow
            {
                Date = e.Date, Direction = "Out", Type = "Expense",
                Description = $"{e.Category}: {e.Description}", Reference = "", Amount = e.Amount
            });
        }

        return rows;
    }

    public List<CashFlowLedgerRow> GetFilteredRows()
    {
        IEnumerable<CashFlowLedgerRow> q = AllRows;
        if (ActiveFilter != "All") q = q.Where(r => r.Direction == ActiveFilter);
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var s = SearchText.Trim();
            q = q.Where(r => r.Description.Contains(s, StringComparison.OrdinalIgnoreCase)
                           || r.Reference.Contains(s, StringComparison.OrdinalIgnoreCase)
                           || r.Type.Contains(s, StringComparison.OrdinalIgnoreCase));
        }
        return q.ToList();
    }

    public int TotalPages(List<CashFlowLedgerRow> filtered) => Math.Max(1, (int)Math.Ceiling(filtered.Count / (double)PageSize));

    public List<CashFlowLedgerRow> GetPage(List<CashFlowLedgerRow> filtered)
    {
        var totalPages = TotalPages(filtered);
        if (CurrentPage > totalPages) CurrentPage = totalPages;
        if (CurrentPage < 1) CurrentPage = 1;
        return filtered.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
    }
}
