using PharmacyMS.Application.Interfaces.Repositories;

namespace PharmacyMS.Desktop.ViewModels;

public class PLSegment
{
    public string Label { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Percent { get; set; }
    public string ColorHex { get; set; } = "#3B82F6";
}

public class PLViewModel
{
    private readonly ISaleRepository _saleRepo;
    private readonly IPurchaseRepository _purchaseRepo;
    private readonly IExpenseRepository _expenseRepo;
    private readonly IReportRepository _reportRepo;

    public DateTime FromDate { get; set; } = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
    public DateTime ToDate { get; set; } = DateTime.Today;

    public decimal TotalRevenue { get; private set; }
    public decimal CostOfGoodsSold { get; private set; }
    public decimal GrossProfit { get; private set; }
    public decimal TotalExpensesAmount { get; private set; }
    public decimal NetProfit { get; private set; }

    public decimal SalesReturnsDiscounts { get; private set; }
    public decimal NetRevenue { get; private set; }
    public decimal OpeningStockValue { get; private set; }
    public decimal PurchasesValue { get; private set; }
    public decimal ClosingStockValue { get; private set; }
    public List<CategoryTotal> ExpenseLines { get; private set; } = new();

    public decimal RevenueChangePercent { get; private set; }
    public decimal CogsChangePercent { get; private set; }
    public decimal GrossProfitChangePercent { get; private set; }
    public decimal ExpensesChangePercent { get; private set; }
    public decimal NetProfitChangePercent { get; private set; }
    public string PriorPeriodLabel { get; private set; } = string.Empty;

    public decimal GrossProfitMargin => NetRevenue > 0 ? GrossProfit / NetRevenue : 0;
    public decimal NetProfitMargin => NetRevenue > 0 ? NetProfit / NetRevenue : 0;
    public decimal ExpenseToRevenueRatio => NetRevenue > 0 ? TotalExpensesAmount / NetRevenue : 0;
    public decimal CostToRevenueRatio => NetRevenue > 0 ? CostOfGoodsSold / NetRevenue : 0;

    public List<PLSegment> DonutSegments { get; private set; } = new();
    public List<TrendPoint> TrendPoints { get; private set; } = new();

    public PLViewModel(ISaleRepository saleRepo, IPurchaseRepository purchaseRepo,
        IExpenseRepository expenseRepo, IReportRepository reportRepo)
    {
        _saleRepo = saleRepo;
        _purchaseRepo = purchaseRepo;
        _expenseRepo = expenseRepo;
        _reportRepo = reportRepo;
    }

    public async Task LoadAsync()
    {
        var current = await ComputeForRangeAsync(FromDate, ToDate);
        TotalRevenue = current.Rev;
        CostOfGoodsSold = current.Cogs;
        GrossProfit = current.Gp;
        TotalExpensesAmount = current.Exp;
        NetProfit = current.Np;
        NetRevenue = current.NetRev;
        SalesReturnsDiscounts = current.Discounts;
        OpeningStockValue = current.Opening;
        PurchasesValue = current.Purchases;
        ClosingStockValue = current.Closing;
        ExpenseLines = current.ExpLines;

        var days = (ToDate.Date - FromDate.Date).Days + 1;
        var priorTo = FromDate.Date.AddDays(-1);
        var priorFrom = priorTo.AddDays(-(days - 1));
        PriorPeriodLabel = $"{priorFrom:MMM d} - {priorTo:MMM d}";
        var prior = await ComputeForRangeAsync(priorFrom, priorTo);
        RevenueChangePercent = PctChange(prior.Rev, current.Rev);
        CogsChangePercent = PctChange(prior.Cogs, current.Cogs);
        GrossProfitChangePercent = PctChange(prior.Gp, current.Gp);
        ExpensesChangePercent = PctChange(prior.Exp, current.Exp);
        NetProfitChangePercent = PctChange(prior.Np, current.Np);

        BuildDonutSegments();

        TrendPoints = new List<TrendPoint>();
        var monthCursor = new DateTime(ToDate.Year, ToDate.Month, 1).AddMonths(-4);
        for (int i = 0; i < 5; i++)
        {
            var mStart = monthCursor;
            var mEndRaw = mStart.AddMonths(1).AddDays(-1);
            var mEnd = mEndRaw > DateTime.Today ? DateTime.Today : mEndRaw;
            var monthResult = await ComputeForRangeAsync(mStart, mEnd);
            TrendPoints.Add(new TrendPoint { Label = $"{mStart:MMM} '{mStart:yy}", Amount = monthResult.Np });
            monthCursor = monthCursor.AddMonths(1);
        }
    }

    private async Task<(decimal Rev, decimal Cogs, decimal Gp, decimal Exp, decimal Np, decimal NetRev,
        decimal Discounts, decimal Opening, decimal Purchases, decimal Closing, List<CategoryTotal> ExpLines)>
        ComputeForRangeAsync(DateTime from, DateTime to)
    {
        var sales = await _saleRepo.GetByDateRangeAsync(from, to);
        var grossRevenue = sales.Sum(s => s.Subtotal);
        var discounts = sales.Sum(s => s.TotalDiscount);
        var netRevenue = grossRevenue - discounts;

        var purchasesInRange = (await _purchaseRepo.GetAllAsync())
            .Where(p => p.CreatedAt.Date >= from.Date && p.CreatedAt.Date <= to.Date)
            .Sum(p => p.TotalAmount);

        var costMap = (await _reportRepo.GetInventoryValuationAsync())
            .GroupBy(r => r.MedicineName)
            .ToDictionary(g => g.Key, g => (decimal)g.First().CostPrice);
        var reconciliation = await _reportRepo.GetMonthlyStockReconciliationAsync(from, to);

        decimal openingValue = 0, closingValue = 0;
        foreach (var row in reconciliation)
        {
            var cost = !string.IsNullOrEmpty(row.MedicineName) && costMap.TryGetValue(row.MedicineName, out var cp) ? cp : 0m;
            openingValue += row.OpeningStock * cost;
            closingValue += row.ClosingStock * cost;
        }

        var cogs = openingValue + purchasesInRange - closingValue;
        var grossProfit = netRevenue - cogs;

        var expenses = (await _expenseRepo.GetByDateRangeAsync(from, to)).ToList();
        var expLines = expenses.GroupBy(e => e.Category)
            .Select(g => new CategoryTotal { Category = g.Key, Amount = g.Sum(e => e.Amount) })
            .OrderByDescending(c => c.Amount).ToList();
        var totalExpenses = expenses.Sum(e => e.Amount);

        var netProfit = grossProfit - totalExpenses;

        return (grossRevenue, cogs, grossProfit, totalExpenses, netProfit, netRevenue, discounts,
            openingValue, purchasesInRange, closingValue, expLines);
    }

    private static decimal PctChange(decimal prior, decimal current)
    {
        if (prior != 0) return (current - prior) / Math.Abs(prior);
        return current == 0 ? 0m : 1m;
    }

    private void BuildDonutSegments()
    {
        var baseRev = TotalRevenue > 0 ? TotalRevenue : 1;
        DonutSegments = new List<PLSegment>
        {
            new() { Label = "Gross Profit", Amount = GrossProfit, Percent = GrossProfit / baseRev, ColorHex = "#10B981" },
            new() { Label = "Cost of Goods Sold", Amount = CostOfGoodsSold, Percent = CostOfGoodsSold / baseRev, ColorHex = "#EF4444" },
            new() { Label = "Total Expenses", Amount = TotalExpensesAmount, Percent = TotalExpensesAmount / baseRev, ColorHex = "#8B5CF6" },
            new() { Label = "Net Profit", Amount = NetProfit, Percent = NetProfit / baseRev, ColorHex = "#3B82F6" },
            new() { Label = "Other Deductions", Amount = SalesReturnsDiscounts, Percent = SalesReturnsDiscounts / baseRev, ColorHex = "#F59E0B" },
        };
    }
}
