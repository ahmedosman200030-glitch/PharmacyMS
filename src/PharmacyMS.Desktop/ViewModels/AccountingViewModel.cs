using System.Collections.ObjectModel;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Desktop.ViewModels;

public class AccountingTransaction
{
    public string Type { get; set; } = string.Empty;      // Income / Expense / Purchase
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
}

public class ChartPoint
{
    public string Month { get; set; } = string.Empty;
    public decimal Income { get; set; }
    public decimal Expenses { get; set; }
    public decimal Profit { get; set; }
}

public class CashFlowRow
{
    public DateTime Date { get; set; }
    public decimal CashIn { get; set; }
    public decimal CashOut { get; set; }
    public decimal Net { get; set; }
}

public class AccountingViewModel
{
    private readonly ISaleRepository _saleRepo;
    private readonly IPurchaseRepository _purchaseRepo;
    private readonly IExpenseRepository _expenseRepo;
    private readonly ICustomerRepository _customerRepo;
    private readonly IMedicineRepository _medicineRepo;

    public DateTime FromDate { get; set; } = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
    public DateTime ToDate { get; set; } = DateTime.Today;

    // Stat cards row 1
    public decimal TotalRevenue { get; private set; }
    public decimal TotalPurchases { get; private set; }
    public decimal GrossProfit => TotalRevenue - TotalPurchases;
    public decimal TotalExpenses { get; private set; }
    public decimal NetProfit => GrossProfit - TotalExpenses;

    // Stat cards row 2
    public decimal CashBalance { get; private set; }
    public decimal CustomerCredit { get; private set; }
    public decimal SupplierPayables { get; private set; }
    public decimal StockValue { get; private set; }

    // Cash flow
    public decimal CashIn { get; private set; }
    public decimal CashOut { get; private set; }

    // Collections
    public ObservableCollection<Expense> Expenses { get; } = new();
    public ObservableCollection<AccountingTransaction> RecentTransactions { get; } = new();
    public ObservableCollection<CashFlowRow> CashFlowRows { get; } = new();
    public ObservableCollection<Sale> IncomeList { get; } = new();
    public ObservableCollection<Sale> ReceivablesList { get; } = new();
    public ObservableCollection<Purchase> PayablesList { get; } = new();
    public List<ChartPoint> ChartPoints { get; private set; } = new();

    public static readonly string[] PredefinedCategories =
    {
        "Rent", "Salaries", "Utilities", "Transport",
        "Maintenance", "Marketing", "Office Supplies",
        "Insurance", "Taxes", "Other"
    };

    public AccountingViewModel(
        ISaleRepository saleRepo,
        IPurchaseRepository purchaseRepo,
        IExpenseRepository expenseRepo,
        ICustomerRepository customerRepo,
        IMedicineRepository medicineRepo)
    {
        _saleRepo = saleRepo;
        _purchaseRepo = purchaseRepo;
        _expenseRepo = expenseRepo;
        _customerRepo = customerRepo;
        _medicineRepo = medicineRepo;
    }

    public async Task LoadAsync()
    {
        var sales = await _saleRepo.GetByDateRangeAsync(FromDate, ToDate);
        var allPurchases = (await _purchaseRepo.GetAllAsync()).ToList();
        var purchases = allPurchases
            .Where(p => p.CreatedAt >= FromDate && p.CreatedAt <= ToDate.AddDays(1)).ToList();
        var expenses = (await _expenseRepo.GetByDateRangeAsync(FromDate, ToDate)).ToList();
        var medicines = (await _medicineRepo.GetAllAsync()).ToList();

        // Row 1
        TotalRevenue = sales.Sum(s => s.TotalAmount);
        TotalPurchases = purchases.Sum(p => p.TotalAmount);
        TotalExpenses = expenses.Sum(e => e.Amount);

        // Row 2
        CashIn = sales.Sum(s => s.AmountPaid);
        CashOut = purchases.Sum(p => p.AmountPaid) + TotalExpenses;
        CashBalance = CashIn - CashOut;
        // Customer credit = total sales amount minus total paid across all credit sales
        var creditSales = await _saleRepo.GetCreditSalesAsync();
        CustomerCredit = creditSales.Sum(s => s.TotalAmount - s.AmountPaid);
        SupplierPayables = allPurchases.Sum(p => p.DueAmount);
        StockValue = medicines.Sum(m => m.CostPrice * m.QuantityInStock);

        // Expenses list
        Expenses.Clear();
        foreach (var e in expenses) Expenses.Add(e);

        // Recent transactions (last 10)
        RecentTransactions.Clear();
        var txns = new List<AccountingTransaction>();
        foreach (var s in sales.OrderByDescending(x => x.CreatedAt).Take(5))
            txns.Add(new AccountingTransaction { Type = "Income", Description = $"Sale #{s.InvoiceNumber}", Amount = s.TotalAmount, Date = s.CreatedAt });
        foreach (var p in purchases.OrderByDescending(x => x.CreatedAt).Take(3))
            txns.Add(new AccountingTransaction { Type = "Purchase", Description = $"Purchase from {p.SupplierName}", Amount = p.TotalAmount, Date = p.CreatedAt });
        foreach (var e in expenses.OrderByDescending(x => x.Date).Take(3))
            txns.Add(new AccountingTransaction { Type = "Expense", Description = $"{e.Category}: {e.Description}", Amount = e.Amount, Date = e.Date });
        foreach (var t in txns.OrderByDescending(x => x.Date).Take(10))
            RecentTransactions.Add(t);

        // Income list (all sales in range)
        IncomeList.Clear();
        foreach (var s in sales.OrderByDescending(x => x.CreatedAt)) IncomeList.Add(s);

        // Receivables (credit sales with a balance due)
        ReceivablesList.Clear();
        foreach (var cs in creditSales.Where(s => s.TotalAmount - s.AmountPaid > 0)) ReceivablesList.Add(cs);

        // Payables (purchases with a balance due)
        PayablesList.Clear();
        foreach (var p in allPurchases.Where(p => p.DueAmount > 0)) PayablesList.Add(p);

        // Cash flow rows
        CashFlowRows.Clear();
        var allDates = sales.Select(s => s.CreatedAt.Date)
            .Concat(purchases.Select(p => p.CreatedAt.Date))
            .Concat(expenses.Select(e => e.Date.Date))
            .Distinct().OrderByDescending(d => d);
        foreach (var date in allDates)
        {
            var dayIn = sales.Where(s => s.CreatedAt.Date == date).Sum(s => s.AmountPaid);
            var dayOut = purchases.Where(p => p.CreatedAt.Date == date).Sum(p => p.AmountPaid)
                       + expenses.Where(e => e.Date.Date == date).Sum(e => e.Amount);
            CashFlowRows.Add(new CashFlowRow { Date = date, CashIn = dayIn, CashOut = dayOut, Net = dayIn - dayOut });
        }

        // Chart: last 6 months
        ChartPoints = new List<ChartPoint>();
        for (int i = 5; i >= 0; i--)
        {
            var month = DateTime.Today.AddMonths(-i);
            var mStart = new DateTime(month.Year, month.Month, 1);
            var mEnd = mStart.AddMonths(1).AddDays(-1);
            var allSales2 = await _saleRepo.GetByDateRangeAsync(mStart, mEnd);
            var mPurchases = allPurchases.Where(p => p.CreatedAt >= mStart && p.CreatedAt <= mEnd).ToList();
            var mExpenses = (await _expenseRepo.GetByDateRangeAsync(mStart, mEnd)).ToList();
            var mIncome = allSales2.Sum(s => s.TotalAmount);
            var mCogs = mPurchases.Sum(p => p.TotalAmount);
            var mExp = mExpenses.Sum(e => e.Amount);
            ChartPoints.Add(new ChartPoint
            {
                Month = month.ToString("MMM"),
                Income = mIncome,
                Expenses = mCogs + mExp,
                Profit = mIncome - mCogs - mExp
            });
        }
    }

    public async Task AddExpenseAsync(Expense expense)
    {
        var id = await _expenseRepo.CreateAsync(expense);
        expense.Id = id;
        Expenses.Insert(0, expense);
        TotalExpenses += expense.Amount;
    }

    public async Task DeleteExpenseAsync(Expense expense)
    {
        await _expenseRepo.DeleteAsync(expense.Id);
        Expenses.Remove(expense);
        TotalExpenses -= expense.Amount;
    }
}
