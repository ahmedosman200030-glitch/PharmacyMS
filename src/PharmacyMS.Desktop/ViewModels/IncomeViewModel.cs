using PharmacyMS.Application.Interfaces.Repositories;

namespace PharmacyMS.Desktop.ViewModels;

public class IncomeRow
{
    public DateTime Date { get; set; }
    public string Type { get; set; } = string.Empty; // "Cash Sales" or "Customer Payment"
    public string Description { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string ReceivedBy { get; set; } = string.Empty;
}

public class IncomeTrendPoint
{
    public string Label { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class IncomeViewModel
{
    public static readonly string[] PredefinedIncomeCategories =
    {
        "Service Income", "Consultation Fee", "Delivery Charge", "Rental Income", "Other Income"
    };

    private readonly ISaleRepository _saleRepo;
    private readonly IOtherIncomeRepository _otherIncomeRepo;

    public DateTime FromDate { get; set; } = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
    public DateTime ToDate { get; set; } = DateTime.Today;

    public decimal TotalIncome { get; private set; }
    public decimal CashSalesTotal { get; private set; }
    public decimal CustomerPaymentsTotal { get; private set; }
    public decimal OtherIncomeTotal { get; private set; }
    public int TransactionCount { get; private set; }

    public List<IncomeRow> AllRows { get; private set; } = new();
    public List<IncomeTrendPoint> TrendPoints { get; private set; } = new();

    public string ActiveFilter { get; set; } = "All"; // All | Cash Sales | Customer Payment | Other Income
    public string SearchText { get; set; } = string.Empty;
    public int PageSize { get; } = 8;
    public int CurrentPage { get; set; } = 1;

    public IncomeViewModel(ISaleRepository saleRepo, IOtherIncomeRepository otherIncomeRepo)
    {
        _saleRepo = saleRepo;
        _otherIncomeRepo = otherIncomeRepo;
    }

    public async Task LoadAsync()
    {
        var sales = await _saleRepo.GetByDateRangeAsync(FromDate, ToDate);
        var rows = new List<IncomeRow>();

        foreach (var s in sales)
        {
            if (s.AmountPaid > 0)
            {
                rows.Add(new IncomeRow
                {
                    Date = s.CreatedAt,
                    Type = "Cash Sales",
                    Description = $"Sale to {s.CustomerName}",
                    Reference = s.InvoiceNumber,
                    Amount = s.AmountPaid,
                    PaymentMethod = s.PaymentMethod,
                    ReceivedBy = "Staff"
                });
            }
        }

        var creditSales = await _saleRepo.GetCreditSalesAsync();
        foreach (var cs in creditSales)
        {
            var payments = await _saleRepo.GetPaymentsAsync(cs.Id);
            foreach (var p in payments.Where(p => p.PaidAt >= FromDate && p.PaidAt <= ToDate.AddDays(1)))
            {
                rows.Add(new IncomeRow
                {
                    Date = p.PaidAt,
                    Type = "Customer Payment",
                    Description = $"Payment from {cs.CustomerName}",
                    Reference = cs.InvoiceNumber,
                    Amount = p.Amount,
                    PaymentMethod = "—",
                    ReceivedBy = "Staff"
                });
            }
        }

        // Other Income — non-sale sources (service fees, consultations, delivery
        // charges, etc). Intentionally NOT included in P&L / Dashboard / Accounting
        // Overview revenue — this total only feeds this Income screen.
        var otherIncome = await _otherIncomeRepo.GetByDateRangeAsync(FromDate, ToDate);
        foreach (var oi in otherIncome)
        {
            rows.Add(new IncomeRow
            {
                Date = oi.Date,
                Type = "Other Income",
                Description = string.IsNullOrWhiteSpace(oi.Description) ? oi.Category : $"{oi.Category} — {oi.Description}",
                Reference = oi.Category,
                Amount = oi.Amount,
                PaymentMethod = "—",
                ReceivedBy = oi.CreatedBy
            });
        }

        AllRows = rows.OrderByDescending(r => r.Date).ToList();

        CashSalesTotal = rows.Where(r => r.Type == "Cash Sales").Sum(r => r.Amount);
        CustomerPaymentsTotal = rows.Where(r => r.Type == "Customer Payment").Sum(r => r.Amount);
        OtherIncomeTotal = rows.Where(r => r.Type == "Other Income").Sum(r => r.Amount);
        TotalIncome = CashSalesTotal + CustomerPaymentsTotal + OtherIncomeTotal;
        TransactionCount = rows.Count;

        TrendPoints = new List<IncomeTrendPoint>();
        var days = (ToDate.Date - FromDate.Date).Days;
        if (days is >= 0 and <= 62)
        {
            for (var d = FromDate.Date; d <= ToDate.Date; d = d.AddDays(1))
            {
                var dayTotal = rows.Where(r => r.Date.Date == d).Sum(r => r.Amount);
                TrendPoints.Add(new IncomeTrendPoint { Label = d.ToString("MMM d"), Amount = dayTotal });
            }
        }
    }

    public List<IncomeRow> GetFilteredRows()
    {
        IEnumerable<IncomeRow> q = AllRows;
        if (ActiveFilter != "All")
            q = q.Where(r => r.Type == ActiveFilter);
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var s = SearchText.Trim();
            q = q.Where(r => r.Description.Contains(s, StringComparison.OrdinalIgnoreCase)
                           || r.Reference.Contains(s, StringComparison.OrdinalIgnoreCase));
        }
        return q.ToList();
    }

    public int TotalPages(List<IncomeRow> filtered) =>
        Math.Max(1, (int)Math.Ceiling(filtered.Count / (double)PageSize));

    public List<IncomeRow> GetPage(List<IncomeRow> filtered)
    {
        var totalPages = TotalPages(filtered);
        if (CurrentPage > totalPages) CurrentPage = totalPages;
        if (CurrentPage < 1) CurrentPage = 1;
        return filtered.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
    }

    public async Task<int> AddOtherIncomeAsync(PharmacyMS.Domain.Entities.OtherIncome income) =>
        await _otherIncomeRepo.CreateAsync(income);
}
