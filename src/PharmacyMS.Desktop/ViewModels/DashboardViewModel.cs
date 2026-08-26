using System.Collections.ObjectModel;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Desktop.ViewModels;

public enum ChartRange
{
    Last7Days,
    Last4Weeks,
    Last6Months
}

public class LowStockRow
{
    private static readonly Dictionary<string, string> NameToImage = new(StringComparer.OrdinalIgnoreCase)
    {
        { "paracetamol",  "avares://PharmacyMS.Desktop/Assets/Medicines/paracetamol.png" },
        { "amoxicillin",  "avares://PharmacyMS.Desktop/Assets/Medicines/amoxicillin.png" },
        { "ciprofloxacin","avares://PharmacyMS.Desktop/Assets/Medicines/ciprofloxacin.png" },
        { "diclofenac",   "avares://PharmacyMS.Desktop/Assets/Medicines/diclofenac.png" },
        { "azithromycin", "avares://PharmacyMS.Desktop/Assets/Medicines/azithromycin.png" },
    };

    private static readonly string[] FallbackColors = {
        "#FEE2E2","#DBEAFE","#D1FAE5","#EDE9FE","#FEF3C7","#FCE7F3","#CCFBF1","#FFF7ED"
    };

    private static readonly Dictionary<string, Avalonia.Media.Imaging.Bitmap?> _cache = new();

    public string Name { get; set; } = "";
    public int Stock { get; set; }
    public bool IsVeryLow { get; set; }

    public string StatusText => Stock == 0 ? "Out of Stock" : (IsVeryLow ? "Very Low" : "Low");
    public string StatusBgColor => Stock == 0 ? "#FEE2E2" : (IsVeryLow ? "#FEE2E2" : "#FEE2E2");
    public string StatusTextColor => Stock == 0 ? "#DC2626" : (IsVeryLow ? "#DC2626" : "#EF4444");
    public string RemainingText => $"Remaining: {Stock}";
    public string FallbackColor => FallbackColors[Math.Abs(Name.GetHashCode()) % FallbackColors.Length];

    private Avalonia.Media.Imaging.Bitmap? _image;
    private bool _imageLoaded = false;

    public Avalonia.Media.Imaging.Bitmap? MedicineImage
    {
        get
        {
            if (_imageLoaded) return _image;
            _imageLoaded = true;
            foreach (var kv in NameToImage)
            {
                if (!Name.Contains(kv.Key, StringComparison.OrdinalIgnoreCase)) continue;
                if (_cache.TryGetValue(kv.Key, out var cached))
                {
                    _image = cached;
                    return _image;
                }
                try
                {
                    var uri = new Uri(kv.Value);
                    using var stream = Avalonia.Platform.AssetLoader.Open(uri);
                    var bmp = new Avalonia.Media.Imaging.Bitmap(stream);
                    _cache[kv.Key] = bmp;
                    _image = bmp;
                    return _image;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"LowStockRow image load failed for {kv.Key}: {ex.Message}");
                }
            }
            return null;
        }
    }

    public bool HasImage => MedicineImage != null;
}

public class SalesPoint
{
    public string Label { get; set; } = "";
    public decimal Amount { get; set; }
}

public class PaymentMethodSlice
{
    public string Name { get; set; } = "";
    public decimal Amount { get; set; }
    public double Percent { get; set; }
    public string Color { get; set; } = "#EF4444";
    public string AmountText => $"${Amount:N2}";
    public string PercentText => $"{Percent:F1}%";
}

public class TopMedicineRow
{
    private static readonly Dictionary<string, string> NameToImage = new(StringComparer.OrdinalIgnoreCase)
    {
        { "paracetamol",  "avares://PharmacyMS.Desktop/Assets/Medicines/paracetamol.png" },
        { "amoxicillin",  "avares://PharmacyMS.Desktop/Assets/Medicines/amoxicillin.png" },
        { "ciprofloxacin","avares://PharmacyMS.Desktop/Assets/Medicines/ciprofloxacin.png" },
        { "diclofenac",   "avares://PharmacyMS.Desktop/Assets/Medicines/diclofenac.png" },
        { "azithromycin", "avares://PharmacyMS.Desktop/Assets/Medicines/azithromycin.png" },
    };

    private static readonly string[] FallbackColors = {
        "#FEE2E2","#DBEAFE","#D1FAE5","#EDE9FE","#FEF3C7","#FCE7F3","#CCFBF1","#FFF7ED"
    };

    private static readonly Dictionary<string, Avalonia.Media.Imaging.Bitmap?> _cache = new();

    public string Name { get; set; } = "";
    public int QuantitySold { get; set; }

    private Avalonia.Media.Imaging.Bitmap? _image;
    private bool _imageLoaded = false;

    public Avalonia.Media.Imaging.Bitmap? MedicineImage
    {
        get
        {
            if (_imageLoaded) return _image;
            _imageLoaded = true;
            foreach (var kv in NameToImage)
            {
                if (!Name.Contains(kv.Key, StringComparison.OrdinalIgnoreCase)) continue;
                if (_cache.TryGetValue(kv.Key, out var cached))
                {
                    _image = cached;
                    return _image;
                }
                try
                {
                    var uri = new Uri(kv.Value);
                    using var stream = Avalonia.Platform.AssetLoader.Open(uri);
                    var bmp = new Avalonia.Media.Imaging.Bitmap(stream);
                    _cache[kv.Key] = bmp;
                    _image = bmp;
                    return _image;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"TopMedicineRow image load failed for {kv.Key}: {ex.Message}");
                }
            }
            return null;
        }
    }

    public bool HasImage => MedicineImage != null;
    public string FallbackColor => FallbackColors[Math.Abs(Name.GetHashCode()) % FallbackColors.Length];
    public string FallbackEmoji => "💊";
}

public class RecentTransactionRow
{
    public string InvoiceNumber { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public decimal Amount { get; set; }
    public string TimeText { get; set; } = "";
    public string AmountText => $"${Amount:N2}";
}

public class DashboardViewModel
{
    private readonly IMedicineRepository _medicineRepo;
    private readonly IReportRepository _reportRepo;
    private readonly ISaleRepository _saleRepo;
    private readonly IExpenseRepository _expenseRepo;

    private static readonly string[] PaymentColors = { "#EF4444", "#3B82F6", "#22C55E", "#A855F7", "#F59E0B", "#0891B2" };

    public ObservableCollection<LowStockRow> LowStockRows { get; } = new();
    public ObservableCollection<SalesPoint> SalesSeries { get; } = new();
    public ObservableCollection<PaymentMethodSlice> PaymentBreakdown { get; } = new();
    public ObservableCollection<TopMedicineRow> TopMedicines { get; } = new();
    public ObservableCollection<RecentTransactionRow> RecentTransactions { get; } = new();

    public ChartRange CurrentChartRange { get; private set; } = ChartRange.Last7Days;

    public decimal TodayRevenue { get; private set; }
    public decimal TodayProfit { get; private set; }
    public int TodayTransactions { get; private set; }
    public int NewCustomersToday { get; private set; }

    public string RevenueTrendText { get; private set; } = "";
    public bool RevenueTrendUp { get; private set; } = true;
    public string ProfitTrendText { get; private set; } = "";
    public bool ProfitTrendUp { get; private set; } = true;
    public string TransactionsTrendText { get; private set; } = "";
    public bool TransactionsTrendUp { get; private set; } = true;
    public string NewCustomersTrendText { get; private set; } = "";
    public bool NewCustomersTrendUp { get; private set; } = true;

    // Bottom strip stats. OpeningBalance is read from the generic settings store (key
    // "OpeningBalance") since there's no dedicated cash-balance feature yet - it will show
    // 0 until something (e.g. Daily Closing or Settings) writes that setting.
    public decimal OpeningBalance { get; private set; }
    public decimal TodayExpenses { get; private set; }
    public int StockItemsCount { get; private set; }
    public int ExpiredItemsCount { get; private set; }

    public DashboardViewModel(
        IMedicineRepository medicineRepo,
        IReportRepository reportRepo,
        ISaleRepository saleRepo,
        IExpenseRepository expenseRepo)
    {
        _medicineRepo = medicineRepo;
        _reportRepo = reportRepo;
        _saleRepo = saleRepo;
        _expenseRepo = expenseRepo;
    }

    public async Task LoadAsync()
    {
        LowStockRows.Clear();
        PaymentBreakdown.Clear();
        TopMedicines.Clear();
        RecentTransactions.Clear();

        var todayStart = DateTime.Today;
        var todayEnd = DateTime.Today.AddDays(1).AddSeconds(-1);
        var yestStart = DateTime.Today.AddDays(-1);
        var yestEnd = DateTime.Today.AddSeconds(-1);

        // --- Today vs yesterday stat cards ---
        TodayRevenue = await _reportRepo.GetTotalRevenueAsync(todayStart, todayEnd);
        var todayPurchaseCost = await _reportRepo.GetTotalPurchaseCostAsync(todayStart, todayEnd);
        TodayProfit = TodayRevenue - todayPurchaseCost;
        TodayTransactions = await _reportRepo.GetTotalTransactionsAsync(todayStart, todayEnd);

        var yesterdayRevenue = await _reportRepo.GetTotalRevenueAsync(yestStart, yestEnd);
        var yesterdayPurchaseCost = await _reportRepo.GetTotalPurchaseCostAsync(yestStart, yestEnd);
        var yesterdayProfit = yesterdayRevenue - yesterdayPurchaseCost;
        var yesterdayTransactions = await _reportRepo.GetTotalTransactionsAsync(yestStart, yestEnd);

        (RevenueTrendText, RevenueTrendUp) = ComputeTrend(TodayRevenue, yesterdayRevenue);
        (ProfitTrendText, ProfitTrendUp) = ComputeTrend(TodayProfit, yesterdayProfit);
        (TransactionsTrendText, TransactionsTrendUp) = ComputeTrend(TodayTransactions, yesterdayTransactions);

        // --- New customers today (first-ever sale falls today), vs yesterday ---
        var allSales = await _saleRepo.GetAllAsync();
        var firstPurchaseByCustomer = allSales
            .Where(s => s.CustomerId.HasValue)
            .GroupBy(s => s.CustomerId!.Value)
            .Select(g => g.Min(s => s.CreatedAt))
            .ToList();

        NewCustomersToday = firstPurchaseByCustomer.Count(d => d.Date == todayStart);
        var newCustomersYesterday = firstPurchaseByCustomer.Count(d => d.Date == yestStart);
        (NewCustomersTrendText, NewCustomersTrendUp) = ComputeTrend(NewCustomersToday, newCustomersYesterday);

        // --- Bottom strip: opening balance, today's expenses, stock items, expired items ---
        var openingBalanceSetting = await _reportRepo.GetSettingAsync("OpeningBalance");
        OpeningBalance = decimal.TryParse(openingBalanceSetting, out var ob) ? ob : 0m;
        TodayExpenses = await _expenseRepo.GetTotalByDateRangeAsync(todayStart, todayEnd);

        // --- Sales overview chart (default range) ---
        await LoadSalesSeriesAsync(CurrentChartRange);

        // --- Sales by payment method (this month) ---
        var monthSales = await _saleRepo.GetByDateRangeAsync(new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1), todayEnd);
        var byMethod = monthSales
            .GroupBy(s => string.IsNullOrWhiteSpace(s.PaymentMethod) ? "Cash" : s.PaymentMethod)
            .Select(g => (Name: g.Key, Amount: g.Sum(s => s.TotalAmount)))
            .OrderByDescending(x => x.Amount)
            .ToList();

        var totalPayments = byMethod.Sum(x => x.Amount);
        if (totalPayments <= 0) totalPayments = 1;

        int colorIndex = 0;
        foreach (var m in byMethod)
        {
            PaymentBreakdown.Add(new PaymentMethodSlice
            {
                Name = m.Name,
                Amount = m.Amount,
                Percent = (double)(m.Amount / totalPayments * 100m),
                Color = PaymentColors[colorIndex % PaymentColors.Length]
            });
            colorIndex++;
        }

        // --- Top selling medicines (this month) ---
        var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var topSelling = await _reportRepo.GetTopSellingMedicinesAsync(monthStart, todayEnd, 5);
        foreach (var t in topSelling)
            TopMedicines.Add(new TopMedicineRow { Name = t.MedicineName, QuantitySold = t.QuantitySold });

        // --- Recent transactions ---
        var recent = allSales.OrderByDescending(s => s.CreatedAt).Take(5);
        foreach (var s in recent)
        {
            RecentTransactions.Add(new RecentTransactionRow
            {
                InvoiceNumber = s.InvoiceNumber,
                CustomerName = string.IsNullOrWhiteSpace(s.CustomerName) ? "Walk-in Customer" : s.CustomerName,
                Amount = s.TotalAmount,
                TimeText = s.CreatedAt.ToString("hh:mm tt")
            });
        }

        // --- Low stock alert: show actual low stock, or fallback to 5 lowest if none ---
        var allMeds = await _medicineRepo.GetAllAsync();
        var allMedsList = allMeds.ToList();

        StockItemsCount = allMedsList.Count;
        ExpiredItemsCount = allMedsList.Count(m => m.ExpiryDate.HasValue && m.ExpiryDate.Value.Date < DateTime.Today);
        var lowStockMeds = allMedsList
            .Where(m => m.QuantityInStock <= m.ReorderLevel && m.QuantityInStock > 0)
            .OrderBy(m => m.QuantityInStock)
            .ToList();

        // If no medicines are actually low stock, show the 5 with lowest stock as a heads-up
        if (lowStockMeds.Count == 0)
        {
            lowStockMeds = allMedsList
                .Where(m => m.QuantityInStock > 0)
                .OrderBy(m => m.QuantityInStock)
                .Take(5)
                .ToList();
        }

        foreach (var m in lowStockMeds.Take(5))
        {
            LowStockRows.Add(new LowStockRow
            {
                Name = m.Name,
                Stock = m.QuantityInStock,
                IsVeryLow = m.QuantityInStock <= m.ReorderLevel * 2
            });
        }
    }

    /// <summary>
    /// Reloads SalesSeries for the given chart range: daily points for the last 7 days,
    /// trailing 7-day buckets for the last 4 weeks, or calendar months for the last 6 months.
    /// </summary>
    public async Task LoadSalesSeriesAsync(ChartRange range)
    {
        CurrentChartRange = range;
        SalesSeries.Clear();

        var todayEnd = DateTime.Today.AddDays(1).AddSeconds(-1);

        switch (range)
        {
            case ChartRange.Last7Days:
                for (int i = 6; i >= 0; i--)
                {
                    var day = DateTime.Today.AddDays(-i);
                    var dayEnd = day.AddDays(1).AddSeconds(-1);
                    var amount = await _reportRepo.GetTotalRevenueAsync(day, dayEnd);
                    SalesSeries.Add(new SalesPoint { Label = day.ToString("ddd"), Amount = amount });
                }
                break;

            case ChartRange.Last4Weeks:
                for (int i = 3; i >= 0; i--)
                {
                    var weekEnd = DateTime.Today.AddDays(-7 * i);
                    var weekStart = weekEnd.AddDays(-6);
                    var weekEndInclusive = weekEnd.AddDays(1).AddSeconds(-1);
                    var amount = await _reportRepo.GetTotalRevenueAsync(weekStart, weekEndInclusive);
                    SalesSeries.Add(new SalesPoint { Label = weekStart.ToString("MMM d"), Amount = amount });
                }
                break;

            case ChartRange.Last6Months:
                for (int i = 5; i >= 0; i--)
                {
                    var monthAnchor = DateTime.Today.AddMonths(-i);
                    var monthStart = new DateTime(monthAnchor.Year, monthAnchor.Month, 1);
                    var monthEndCalendar = monthStart.AddMonths(1).AddSeconds(-1);
                    var monthEnd = monthEndCalendar < todayEnd ? monthEndCalendar : todayEnd;
                    var amount = await _reportRepo.GetTotalRevenueAsync(monthStart, monthEnd);
                    SalesSeries.Add(new SalesPoint { Label = monthStart.ToString("MMM"), Amount = amount });
                }
                break;
        }
    }

    private static (string text, bool isUp) ComputeTrend(decimal current, decimal previous)
    {
        if (previous == 0)
            return current == 0 ? ("0.0%", true) : ("New", true);
        var change = (current - previous) / previous * 100m;
        var sign = change >= 0 ? "+" : "";
        return ($"{sign}{change:F1}%", change >= 0);
    }

    private static (string text, bool isUp) ComputeTrend(int current, int previous)
        => ComputeTrend((decimal)current, previous);
}
