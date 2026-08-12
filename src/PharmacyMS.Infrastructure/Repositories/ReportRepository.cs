using Dapper;
using PharmacyMS.Application.DTOs;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Infrastructure.Data;

namespace PharmacyMS.Infrastructure.Repositories;

public class ReportRepository : IReportRepository
{
    private readonly AppDbContext _context;

    public ReportRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<decimal> GetTotalRevenueAsync(DateTime from, DateTime to)
    {
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<decimal>(@"
            SELECT COALESCE(SUM(TotalAmount), 0) FROM Sales
            WHERE CreatedAt >= @From AND CreatedAt <= @To",
            new { From = from.ToString("yyyy-MM-dd 00:00:00"), To = to.ToString("yyyy-MM-dd 23:59:59") });
    }

    public async Task<int> GetTotalTransactionsAsync(DateTime from, DateTime to)
    {
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*) FROM Sales
            WHERE CreatedAt >= @From AND CreatedAt <= @To",
            new { From = from.ToString("yyyy-MM-dd 00:00:00"), To = to.ToString("yyyy-MM-dd 23:59:59") });
    }

    public async Task<decimal> GetTotalPurchaseCostAsync(DateTime from, DateTime to)
    {
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<decimal>(@"
            SELECT COALESCE(SUM(TotalAmount), 0) FROM Purchases
            WHERE CreatedAt >= @From AND CreatedAt <= @To",
            new { From = from.ToString("yyyy-MM-dd 00:00:00"), To = to.ToString("yyyy-MM-dd 23:59:59") });
    }

    public async Task<IEnumerable<TopSellingMedicine>> GetTopSellingMedicinesAsync(DateTime from, DateTime to, int topN = 10)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<TopSellingMedicine>(@"
            SELECT si.MedicineName AS MedicineName,
                   SUM(si.Quantity) AS QuantitySold,
                   SUM(si.UnitPrice * si.Quantity) AS Revenue
            FROM SaleItems si
            JOIN Sales s ON si.SaleId = s.Id
            WHERE s.CreatedAt >= @From AND s.CreatedAt <= @To
            GROUP BY si.MedicineName
            ORDER BY QuantitySold DESC
            LIMIT @TopN",
            new
            {
                From = from.ToString("yyyy-MM-dd 00:00:00"),
                To = to.ToString("yyyy-MM-dd 23:59:59"),
                TopN = topN
            });
    }

    public async Task<decimal> GetTotalReceivablesAsync()
    {
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<decimal>(@"
            SELECT COALESCE(SUM(TotalAmount - AmountPaid), 0)
            FROM Sales
            WHERE AmountPaid < TotalAmount");
    }

    public async Task<decimal> GetReceivablesCreatedInRangeAsync(DateTime from, DateTime to)
    {
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<decimal>(@"
            SELECT COALESCE(SUM(TotalAmount - AmountPaid), 0)
            FROM Sales
            WHERE AmountPaid < TotalAmount
              AND CreatedAt >= @From AND CreatedAt <= @To",
            new { From = from.ToString("yyyy-MM-dd 00:00:00"), To = to.ToString("yyyy-MM-dd 23:59:59") });
    }

    private class MonthAggRow
    {
        public string Month { get; set; } = string.Empty;
        public decimal Total { get; set; }
    }

    public async Task<IEnumerable<MonthlySummary>> GetMonthlySalesAndPurchasesAsync(int months = 4)
    {
        using var conn = _context.CreateConnection();

        var salesRows = (await conn.QueryAsync<MonthAggRow>(@"
            SELECT strftime('%Y-%m', CreatedAt) AS Month, COALESCE(SUM(TotalAmount), 0) AS Total
            FROM Sales
            GROUP BY Month"))
            .ToDictionary(r => r.Month, r => r.Total);

        var purchaseRows = (await conn.QueryAsync<MonthAggRow>(@"
            SELECT strftime('%Y-%m', CreatedAt) AS Month, COALESCE(SUM(TotalAmount), 0) AS Total
            FROM Purchases
            GROUP BY Month"))
            .ToDictionary(r => r.Month, r => r.Total);

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

    private class StockMovementRow
    {
        public string MedicineName { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public int ReceivedIn { get; set; }
        public int DispensedIn { get; set; }
        public int AdjustmentsIn { get; set; }
        public int ReceivedAfter { get; set; }
        public int DispensedAfter { get; set; }
        public int AdjustmentsAfter { get; set; }
    }

    public async Task<IEnumerable<MonthlyStockReconciliationRow>> GetMonthlyStockReconciliationAsync(DateTime monthStart, DateTime monthEnd)
    {
        using var conn = _context.CreateConnection();

        var from = monthStart.ToString("yyyy-MM-dd 00:00:00");
        var to = monthEnd.ToString("yyyy-MM-dd 23:59:59");

        var rows = await conn.QueryAsync<StockMovementRow>(@"
            SELECT
                m.Name AS MedicineName,
                m.QuantityInStock AS CurrentStock,
                COALESCE((SELECT SUM(pi.Quantity) FROM PurchaseItems pi
                          JOIN Purchases p ON pi.PurchaseId = p.Id
                          WHERE pi.MedicineId = m.Id AND p.CreatedAt >= @From AND p.CreatedAt <= @To), 0) AS ReceivedIn,
                COALESCE((SELECT SUM(si.Quantity) FROM SaleItems si
                          JOIN Sales s ON si.SaleId = s.Id
                          WHERE si.MedicineId = m.Id AND s.CreatedAt >= @From AND s.CreatedAt <= @To), 0) AS DispensedIn,
                COALESCE((SELECT SUM(sa.QuantityChange) FROM StockAdjustments sa
                          WHERE sa.MedicineId = m.Id AND sa.CreatedAt >= @From AND sa.CreatedAt <= @To), 0) AS AdjustmentsIn,
                COALESCE((SELECT SUM(pi.Quantity) FROM PurchaseItems pi
                          JOIN Purchases p ON pi.PurchaseId = p.Id
                          WHERE pi.MedicineId = m.Id AND p.CreatedAt > @To), 0) AS ReceivedAfter,
                COALESCE((SELECT SUM(si.Quantity) FROM SaleItems si
                          JOIN Sales s ON si.SaleId = s.Id
                          WHERE si.MedicineId = m.Id AND s.CreatedAt > @To), 0) AS DispensedAfter,
                COALESCE((SELECT SUM(sa.QuantityChange) FROM StockAdjustments sa
                          WHERE sa.MedicineId = m.Id AND sa.CreatedAt > @To), 0) AS AdjustmentsAfter
            FROM Medicines m
            WHERE m.IsActive = 1
            ORDER BY m.Name",
            new { From = from, To = to });

        var result = new List<MonthlyStockReconciliationRow>();
        foreach (var r in rows)
        {
            var closing = r.CurrentStock - r.ReceivedAfter + r.DispensedAfter - r.AdjustmentsAfter;
            var opening = closing - r.ReceivedIn + r.DispensedIn - r.AdjustmentsIn;

            result.Add(new MonthlyStockReconciliationRow
            {
                MedicineName = r.MedicineName,
                OpeningStock = opening,
                Received = r.ReceivedIn,
                Dispensed = r.DispensedIn,
                Adjustments = r.AdjustmentsIn,
                ClosingStock = closing
            });
        }

        return result;
    }

    public async Task<string?> GetSettingAsync(string key)
    {
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<string?>(
            "SELECT Value FROM Settings WHERE Key = @Key", new { Key = key });
    }

    public async Task SetSettingAsync(string key, string value)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(@"
            INSERT INTO Settings (Key, Value) VALUES (@Key, @Value)
            ON CONFLICT(Key) DO UPDATE SET Value = @Value",
            new { Key = key, Value = value });
    }
}
