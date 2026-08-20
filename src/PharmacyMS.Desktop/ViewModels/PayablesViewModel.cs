using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Desktop.ViewModels;

public class PayableRow
{
    private static readonly string[] Palette =
    {
        "#DC2626", "#3B82F6", "#10B981", "#F59E0B", "#8B5CF6",
        "#EC4899", "#14B8A6", "#F97316", "#6366F1", "#84CC16"
    };

    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public decimal TotalPurchases { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal Balance => TotalPurchases - TotalPaid;
    public string Status => Balance <= 0 ? "Paid" : "Outstanding";
    public DateTime? LastPayment { get; set; }
    public List<Purchase> Purchases { get; set; } = new();

    public string Initials
    {
        get
        {
            if (string.IsNullOrWhiteSpace(SupplierName)) return "?";
            var parts = SupplierName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1) return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpperInvariant();
            return (parts[0][0].ToString() + parts[^1][0]).ToUpperInvariant();
        }
    }

    public string AvatarColor
    {
        get
        {
            int hash = 0;
            foreach (var c in SupplierName) hash = (hash * 31 + c) & 0x7fffffff;
            return Palette[hash % Palette.Length];
        }
    }
}

public class PayablesViewModel
{
    private readonly IPurchaseRepository _purchaseRepo;
    private readonly ISupplierRepository _supplierRepo;

    public DateTime FromDate { get; set; } = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
    public DateTime ToDate { get; set; } = DateTime.Today;

    public decimal TotalPayables { get; private set; }
    public decimal TotalPaidThisMonth { get; private set; }
    public decimal OutstandingAmount { get; private set; }
    public decimal AgingOver90 { get; private set; }
    public int SupplierCount { get; private set; }

    public List<PayableRow> AllRows { get; private set; } = new();
    public List<AgingBucket> AgingBuckets { get; private set; } = new();
    public List<TrendPoint> TrendPoints { get; private set; } = new();

    public string ActiveFilter { get; set; } = "All";
    public string SearchText { get; set; } = string.Empty;
    public int PageSize { get; } = 8;
    public int CurrentPage { get; set; } = 1;

    public PayablesViewModel(IPurchaseRepository purchaseRepo, ISupplierRepository supplierRepo)
    {
        _purchaseRepo = purchaseRepo;
        _supplierRepo = supplierRepo;
    }

    public async Task LoadAsync()
    {
        var allPurchases = (await _purchaseRepo.GetAllAsync()).ToList();
        var supplierPurchases = allPurchases.Where(p => p.SupplierId.HasValue).ToList();
        var grouped = supplierPurchases.GroupBy(p => p.SupplierId!.Value).ToList();
        var suppliers = (await _supplierRepo.GetAllAsync()).ToDictionary(s => s.Id);

        var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        decimal paidThisMonth = 0;

        AllRows = new List<PayableRow>();
        foreach (var g in grouped)
        {
            suppliers.TryGetValue(g.Key, out var supplier);
            var row = new PayableRow
            {
                SupplierId = g.Key,
                SupplierName = supplier?.Name ?? g.First().SupplierName,
                Phone = supplier?.Phone ?? "",
                TotalPurchases = g.Sum(p => p.TotalAmount),
                TotalPaid = g.Sum(p => p.AmountPaid),
                Purchases = g.OrderBy(p => p.CreatedAt).ToList()
            };

            DateTime? lastPayment = null;
            foreach (var purchase in g)
            {
                var payments = await _purchaseRepo.GetPaymentsAsync(purchase.Id);
                foreach (var p in payments)
                {
                    if (lastPayment == null || p.PaidAt > lastPayment) lastPayment = p.PaidAt;
                    if (p.PaidAt >= monthStart) paidThisMonth += p.Amount;
                }
            }
            row.LastPayment = lastPayment;
            AllRows.Add(row);
        }

        AllRows = AllRows.OrderByDescending(r => r.Balance).ToList();

        TotalPayables = AllRows.Sum(r => r.TotalPurchases);
        TotalPaidThisMonth = paidThisMonth;
        OutstandingAmount = AllRows.Sum(r => r.Balance);
        SupplierCount = AllRows.Count;

        var bucketLabels = new[] { "Current (0-30 days)", "31-60 days", "61-90 days", "Over 90 days" };
        var bucketAmounts = new decimal[4];
        foreach (var row in AllRows.Where(r => r.Balance > 0))
        {
            var oldestUnpaid = row.Purchases.FirstOrDefault(p => p.TotalAmount - p.AmountPaid > 0);
            var age = oldestUnpaid != null ? (DateTime.Today - oldestUnpaid.CreatedAt.Date).Days : 0;
            var idx = age <= 30 ? 0 : age <= 60 ? 1 : age <= 90 ? 2 : 3;
            bucketAmounts[idx] += row.Balance;
        }
        var totalAging = bucketAmounts.Sum();
        AgingBuckets = bucketLabels.Select((b, i) => new AgingBucket
        {
            Label = b,
            Amount = bucketAmounts[i],
            Percent = totalAging > 0 ? bucketAmounts[i] / totalAging : 0
        }).ToList();
        AgingOver90 = bucketAmounts[3];

        TrendPoints = new List<TrendPoint>();
        var days = (ToDate.Date - FromDate.Date).Days;
        if (days is >= 0 and <= 62)
        {
            var rangePurchases = supplierPurchases.Where(p => p.CreatedAt.Date >= FromDate.Date && p.CreatedAt.Date <= ToDate.Date).ToList();
            for (var d = FromDate.Date; d <= ToDate.Date; d = d.AddDays(1))
            {
                var dayTotal = rangePurchases.Where(p => p.CreatedAt.Date == d).Sum(p => p.TotalAmount - p.AmountPaid);
                TrendPoints.Add(new TrendPoint { Label = d.ToString("MMM d"), Amount = dayTotal });
            }
        }
    }

    public List<PayableRow> GetFilteredRows()
    {
        IEnumerable<PayableRow> q = AllRows;
        if (ActiveFilter == "Outstanding") q = q.Where(r => r.Balance > 0);
        else if (ActiveFilter == "Paid") q = q.Where(r => r.Balance <= 0);
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var s = SearchText.Trim();
            q = q.Where(r => r.SupplierName.Contains(s, StringComparison.OrdinalIgnoreCase)
                           || r.Phone.Contains(s, StringComparison.OrdinalIgnoreCase));
        }
        return q.ToList();
    }

    public int TotalPages(List<PayableRow> filtered) => Math.Max(1, (int)Math.Ceiling(filtered.Count / (double)PageSize));

    public List<PayableRow> GetPage(List<PayableRow> filtered)
    {
        var totalPages = TotalPages(filtered);
        if (CurrentPage > totalPages) CurrentPage = totalPages;
        if (CurrentPage < 1) CurrentPage = 1;
        return filtered.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
    }

    public async Task MakePaymentAsync(PayableRow row, decimal amount)
    {
        var remaining = amount;
        foreach (var purchase in row.Purchases)
        {
            if (remaining <= 0) break;
            var due = purchase.TotalAmount - purchase.AmountPaid;
            if (due <= 0) continue;
            var pay = Math.Min(due, remaining);
            await _purchaseRepo.RecordPaymentAsync(purchase.Id, pay);
            purchase.AmountPaid += pay;
            remaining -= pay;
        }
    }
}
