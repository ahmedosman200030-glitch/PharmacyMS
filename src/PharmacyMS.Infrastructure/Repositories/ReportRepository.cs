using Dapper;
using PharmacyMS.Application.DTOs;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Infrastructure.Data;

namespace PharmacyMS.Infrastructure.Repositories;

public class ReportRepository : IReportRepository
{
    private readonly AppDbContext _context;
    public ReportRepository(AppDbContext context) => _context = context;

    public async Task<decimal> GetTotalRevenueAsync(DateTime from, DateTime to)
    {
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<decimal>(@"
            SELECT COALESCE(SUM(TotalAmount),0) FROM Sales WHERE CreatedAt>=@From AND CreatedAt<=@To",
            new { From = from.ToString("yyyy-MM-dd 00:00:00"), To = to.ToString("yyyy-MM-dd 23:59:59") });
    }

    public async Task<int> GetTotalTransactionsAsync(DateTime from, DateTime to)
    {
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*) FROM Sales WHERE CreatedAt>=@From AND CreatedAt<=@To",
            new { From = from.ToString("yyyy-MM-dd 00:00:00"), To = to.ToString("yyyy-MM-dd 23:59:59") });
    }

    public async Task<decimal> GetTotalPurchaseCostAsync(DateTime from, DateTime to)
    {
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<decimal>(@"
            SELECT COALESCE(SUM(TotalAmount),0) FROM Purchases WHERE CreatedAt>=@From AND CreatedAt<=@To",
            new { From = from.ToString("yyyy-MM-dd 00:00:00"), To = to.ToString("yyyy-MM-dd 23:59:59") });
    }

    public async Task<IEnumerable<TopSellingMedicine>> GetTopSellingMedicinesAsync(DateTime from, DateTime to, int topN = 10)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<TopSellingMedicine>(@"
            SELECT si.MedicineName, SUM(si.Quantity) AS QuantitySold, SUM(si.UnitPrice*si.Quantity) AS Revenue
            FROM SaleItems si JOIN Sales s ON si.SaleId=s.Id
            WHERE s.CreatedAt>=@From AND s.CreatedAt<=@To
            GROUP BY si.MedicineName ORDER BY QuantitySold DESC LIMIT @TopN",
            new { From = from.ToString("yyyy-MM-dd 00:00:00"), To = to.ToString("yyyy-MM-dd 23:59:59"), TopN = topN });
    }

    public async Task<decimal> GetTotalReceivablesAsync()
    {
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<decimal>(@"
            SELECT COALESCE(SUM(TotalAmount-AmountPaid),0) FROM Sales WHERE AmountPaid<TotalAmount");
    }

    public async Task<decimal> GetReceivablesCreatedInRangeAsync(DateTime from, DateTime to)
    {
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<decimal>(@"
            SELECT COALESCE(SUM(TotalAmount-AmountPaid),0) FROM Sales
            WHERE AmountPaid<TotalAmount AND CreatedAt>=@From AND CreatedAt<=@To",
            new { From = from.ToString("yyyy-MM-dd 00:00:00"), To = to.ToString("yyyy-MM-dd 23:59:59") });
    }

    public async Task<IEnumerable<MonthlySummary>> GetMonthlySalesAndPurchasesAsync(int months = 4)
    {
        using var conn = _context.CreateConnection();
        var ymSales = _context.YearMonthExpr("CreatedAt");
        var ymPurch = _context.YearMonthExpr("CreatedAt");
        var salesRows = (await conn.QueryAsync<(string Month, decimal Total)>($@"
            SELECT {ymSales} AS Month, COALESCE(SUM(TotalAmount),0) AS Total
            FROM Sales GROUP BY Month")).ToDictionary(r => r.Month, r => r.Total);
        var purchaseRows = (await conn.QueryAsync<(string Month, decimal Total)>($@"
            SELECT {ymPurch} AS Month, COALESCE(SUM(TotalAmount),0) AS Total
            FROM Purchases GROUP BY Month")).ToDictionary(r => r.Month, r => r.Total);
        var result = new List<MonthlySummary>();
        var cursor = DateTime.Today.AddMonths(-(months - 1));
        for (int i = 0; i < months; i++)
        {
            var key = cursor.ToString("yyyy-MM");
            result.Add(new MonthlySummary
            {
                MonthLabel = cursor.ToString("MMMM"),
                SalesTotal = salesRows.TryGetValue(key, out var s) ? s : 0,
                PurchaseTotal = purchaseRows.TryGetValue(key, out var p) ? p : 0
            });
            cursor = cursor.AddMonths(1);
        }
        return result;
    }

    public async Task<IEnumerable<MonthlyStockReconciliationRow>> GetMonthlyStockReconciliationAsync(DateTime monthStart, DateTime monthEnd)
    {
        using var conn = _context.CreateConnection();
        var from = monthStart.ToString("yyyy-MM-dd 00:00:00");
        var to = monthEnd.ToString("yyyy-MM-dd 23:59:59");
        var rows = await conn.QueryAsync<dynamic>(@"
            SELECT m.Name AS MedicineName, m.QuantityInStock AS CurrentStock,
            COALESCE((SELECT SUM(pi.Quantity) FROM PurchaseItems pi JOIN Purchases p ON pi.PurchaseId=p.Id WHERE pi.MedicineId=m.Id AND p.CreatedAt>=@From AND p.CreatedAt<=@To),0) AS ReceivedIn,
            COALESCE((SELECT SUM(si.Quantity) FROM SaleItems si JOIN Sales s ON si.SaleId=s.Id WHERE si.MedicineId=m.Id AND s.CreatedAt>=@From AND s.CreatedAt<=@To),0) AS DispensedIn,
            COALESCE((SELECT SUM(sa.QuantityChange) FROM StockAdjustments sa WHERE sa.MedicineId=m.Id AND sa.CreatedAt>=@From AND sa.CreatedAt<=@To),0) AS AdjustmentsIn,
            COALESCE((SELECT SUM(pi.Quantity) FROM PurchaseItems pi JOIN Purchases p ON pi.PurchaseId=p.Id WHERE pi.MedicineId=m.Id AND p.CreatedAt>@To),0) AS ReceivedAfter,
            COALESCE((SELECT SUM(si.Quantity) FROM SaleItems si JOIN Sales s ON si.SaleId=s.Id WHERE si.MedicineId=m.Id AND s.CreatedAt>@To),0) AS DispensedAfter,
            COALESCE((SELECT SUM(sa.QuantityChange) FROM StockAdjustments sa WHERE sa.MedicineId=m.Id AND sa.CreatedAt>@To),0) AS AdjustmentsAfter
            FROM Medicines m WHERE m.IsActive=1 ORDER BY m.Name",
            new { From = from, To = to });
        static int SafeInt(object? v) => v == null || v is DBNull ? 0 : Convert.ToInt32(v);

        var result = new List<MonthlyStockReconciliationRow>();
        foreach (var r in rows)
        {
            int currentStock = SafeInt(r.CurrentStock);
            int receivedIn = SafeInt(r.ReceivedIn);
            int dispensedIn = SafeInt(r.DispensedIn);
            int adjustmentsIn = SafeInt(r.AdjustmentsIn);
            int receivedAfter = SafeInt(r.ReceivedAfter);
            int dispensedAfter = SafeInt(r.DispensedAfter);
            int adjustmentsAfter = SafeInt(r.AdjustmentsAfter);

            var closing = currentStock - receivedAfter + dispensedAfter - adjustmentsAfter;
            var opening = closing - receivedIn + dispensedIn - adjustmentsIn;
            result.Add(new MonthlyStockReconciliationRow
            {
                MedicineName = (string?)r.MedicineName ?? "", OpeningStock = opening,
                Received = receivedIn, Dispensed = dispensedIn,
                Adjustments = adjustmentsIn, ClosingStock = closing
            });
        }
        return result;
    }

    public async Task<string?> GetSettingAsync(string key)
    {
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<string?>("SELECT Value FROM Settings WHERE Key=@Key", new { Key = key });
    }

    public async Task SetSettingAsync(string key, string value)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(@"
            INSERT INTO Settings (Key,Value) VALUES (@Key,@Value)
            ON CONFLICT(Key) DO UPDATE SET Value=@Value", new { Key = key, Value = value });
    }

    public async Task<IEnumerable<UserActivityRow>> GetUserActivityAsync(DateTime from, DateTime to)
    {
        using var conn = _context.CreateConnection();
        var fromStr = from.ToString("yyyy-MM-dd 00:00:00");
        var toStr = to.ToString("yyyy-MM-dd 23:59:59");

        var sessions = (await conn.QueryAsync<UserActivityRow>($@"
            SELECT UserId, UserName, MIN(LoginTime) AS LoginTime, MAX(LogoutTime) AS LogoutTime
            FROM UserSessions
            WHERE LoginTime >= @From AND LoginTime <= @To
            GROUP BY UserId, UserName
            ORDER BY MIN(LoginTime) DESC",
            new { From = fromStr, To = toStr })).ToList();

        var salesByUser = (await conn.QueryAsync(
            @"SELECT CashierId, COUNT(*) AS Txns, COALESCE(SUM(TotalAmount),0) AS Revenue
              FROM Sales WHERE CreatedAt >= @From AND CreatedAt <= @To
              GROUP BY CashierId",
            new { From = fromStr, To = toStr }))
            .ToDictionary(r => (int)r.CashierId, r => ((int)r.Txns, (double)r.Revenue));

        foreach (var row in sessions)
        {
            if (salesByUser.TryGetValue(row.UserId, out var s))
            {
                row.Transactions = s.Item1;
                row.SalesAmount = s.Item2;
            }
        }

        return sessions;
    }

    public async Task<IEnumerable<DailySalesSummaryRow>> GetDailySalesAsync(DateTime from, DateTime to)
    {
        using var conn = _context.CreateConnection();
        var groupExpr = _context.DateExpr("CreatedAt");
        var displayExpr = _context.DateDisplayExpr("CreatedAt");
        var rows = (await conn.QueryAsync<DailySalesSummaryRow>($@"
            SELECT {displayExpr} AS Date,
                   COUNT(*) AS Transactions,
                   COALESCE(SUM(TotalAmount),0) AS Revenue,
                   COALESCE(SUM(TotalDiscount),0) AS Discount,
                   COALESCE(SUM(TaxAmount),0) AS Tax,
                   COALESCE(SUM(TotalAmount-TaxAmount),0) AS NetRevenue
            FROM Sales WHERE CreatedAt>=@From AND CreatedAt<=@To
            GROUP BY {groupExpr} ORDER BY {groupExpr} DESC",
            new { From = from.ToString("yyyy-MM-dd 00:00:00"), To = to.ToString("yyyy-MM-dd 23:59:59") })).ToList();
        foreach (var r in rows) r.Date = r.Date.Replace('/', '-');
        return rows;
    }

    public async Task<IEnumerable<PaymentMethodBreakdownRow>> GetPaymentMethodBreakdownAsync(DateTime from, DateTime to)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<PaymentMethodBreakdownRow>(@"
            SELECT PaymentMethod AS Method, COUNT(*) AS Count, COALESCE(SUM(TotalAmount),0) AS Total
            FROM Sales WHERE CreatedAt>=@From AND CreatedAt<=@To
            GROUP BY PaymentMethod ORDER BY Total DESC",
            new { From = from.ToString("yyyy-MM-dd 00:00:00"), To = to.ToString("yyyy-MM-dd 23:59:59") });
    }

    public async Task<IEnumerable<TaxReportRow>> GetTaxReportAsync(DateTime from, DateTime to)
    {
        using var conn = _context.CreateConnection();
        var groupExpr = _context.DateExpr("CreatedAt");
        var displayExpr = _context.DateDisplayExpr("CreatedAt");
        var rows = (await conn.QueryAsync<TaxReportRow>($@"
            SELECT {displayExpr} AS Date,
                   CAST(COALESCE(SUM(TotalAmount),0) AS REAL) AS Revenue,
                   CAST(COALESCE(SUM(TaxAmount),0) AS REAL) AS TaxAmount,
                   CAST(COALESCE(AVG(TaxRate)*100,0) AS REAL) AS TaxRate
            FROM Sales WHERE CreatedAt>=@From AND CreatedAt<=@To
            GROUP BY {groupExpr} ORDER BY {groupExpr} DESC",
            new { From = from.ToString("yyyy-MM-dd 00:00:00"), To = to.ToString("yyyy-MM-dd 23:59:59") })).ToList();
        foreach (var r in rows) r.Date = r.Date.Replace('/', '-');
        return rows;
    }

    public async Task<IEnumerable<InventoryValuationRow>> GetInventoryValuationAsync()
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<InventoryValuationRow>(@"
            SELECT m.Name AS MedicineName,
                   COALESCE(m.Category,'Uncategorized') AS Category,
                   m.QuantityInStock AS Quantity,
                   m.CostPrice,
                   m.UnitPrice AS RetailPrice,
                   m.QuantityInStock * m.CostPrice AS CostValue,
                   m.QuantityInStock * m.UnitPrice AS RetailValue,
                   m.QuantityInStock * (m.UnitPrice - m.CostPrice) AS PotentialProfit
            FROM Medicines m
            WHERE m.QuantityInStock > 0
            ORDER BY RetailValue DESC");
    }

    public async Task<IEnumerable<PurchaseVsSalesRow>> GetPurchaseVsSalesAsync(DateTime from, DateTime to)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<PurchaseVsSalesRow>(@"
            SELECT m.Name AS MedicineName,
                   CAST(COALESCE(pi_sum.Purchased,0) AS REAL) AS Purchased,
                   CAST(COALESCE(si_sum.Sold,0) AS REAL) AS Sold,
                   CAST(COALESCE(pi_sum.PurchaseCost,0) AS REAL) AS PurchaseCost,
                   CAST(COALESCE(si_sum.SaleRevenue,0) AS REAL) AS SaleRevenue,
                   CAST(COALESCE(si_sum.SaleRevenue,0) - COALESCE(pi_sum.PurchaseCost,0) AS REAL) AS Profit
            FROM Medicines m
            LEFT JOIN (
                SELECT pi.MedicineId, SUM(pi.Quantity) AS Purchased, SUM(pi.Quantity*pi.UnitCost) AS PurchaseCost
                FROM PurchaseItems pi JOIN Purchases p ON pi.PurchaseId=p.Id
                WHERE p.CreatedAt>=@From AND p.CreatedAt<=@To
                GROUP BY pi.MedicineId
            ) pi_sum ON m.Id = pi_sum.MedicineId
            LEFT JOIN (
                SELECT si.MedicineId, SUM(si.Quantity) AS Sold, SUM(si.Quantity*si.UnitPrice) AS SaleRevenue
                FROM SaleItems si JOIN Sales s ON si.SaleId=s.Id
                WHERE s.CreatedAt>=@From AND s.CreatedAt<=@To
                GROUP BY si.MedicineId
            ) si_sum ON m.Id = si_sum.MedicineId
            WHERE COALESCE(pi_sum.Purchased,0) > 0 OR COALESCE(si_sum.Sold,0) > 0
            ORDER BY Profit DESC",
            new { From = from.ToString("yyyy-MM-dd 00:00:00"), To = to.ToString("yyyy-MM-dd 23:59:59") });
    }

    public async Task<IEnumerable<SupplierPaymentRow>> GetSupplierPaymentsAsync()
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<SupplierPaymentRow>(@"
            SELECT s.Name AS SupplierName,
                   COALESCE(s.Phone,'—') AS Phone,
                   COUNT(p.Id) AS TotalInvoices,
                   CAST(COALESCE(SUM(p.TotalAmount),0) AS REAL) AS TotalAmount,
                   CAST(COALESCE(SUM(p.AmountPaid),0) AS REAL) AS AmountPaid,
                   CAST(COALESCE(SUM(p.TotalAmount-p.AmountPaid),0) AS REAL) AS Balance,
                   CASE WHEN COALESCE(SUM(p.TotalAmount-p.AmountPaid),0)<=0 THEN 'Paid' ELSE 'Outstanding' END AS Status
            FROM Suppliers s
            LEFT JOIN Purchases p ON s.Id=p.SupplierId
            GROUP BY s.Id, s.Name, s.Phone
            ORDER BY Balance DESC");
    }
}
