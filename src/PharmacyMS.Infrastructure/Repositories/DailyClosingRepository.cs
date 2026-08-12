using Dapper;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Domain.Entities;
using PharmacyMS.Infrastructure.Data;

namespace PharmacyMS.Infrastructure.Repositories;

public class DailyClosingRepository : IDailyClosingRepository
{
    private readonly AppDbContext _context;

    public DailyClosingRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> HasClosedTodayAsync()
    {
        using var conn = _context.CreateConnection();
        var count = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM DailyClosings WHERE ClosingDate = @Today",
            new { Today = DateTime.Today.ToString("yyyy-MM-dd") });
        return count > 0;
    }

    public async Task<int> CreateAsync(DailyClosing closing)
    {
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO DailyClosings
                (ClosingDate, CashSales, CardSales, MobileSales, InsuranceSales, ExpectedCash, ActualCash, Difference, Notes, ClosedByUserId, ClosedByUserName, CreatedAt)
            VALUES
                (@ClosingDate, @CashSales, @CardSales, @MobileSales, @InsuranceSales, @ExpectedCash, @ActualCash, @Difference, @Notes, @ClosedByUserId, @ClosedByUserName, datetime('now'));
            SELECT last_insert_rowid();",
            new
            {
                ClosingDate = closing.ClosingDate.ToString("yyyy-MM-dd"),
                closing.CashSales,
                closing.CardSales,
                closing.MobileSales,
                closing.InsuranceSales,
                closing.ExpectedCash,
                closing.ActualCash,
                closing.Difference,
                closing.Notes,
                closing.ClosedByUserId,
                closing.ClosedByUserName
            });
    }

    public async Task<List<DailyClosing>> GetHistoryAsync()
    {
        using var conn = _context.CreateConnection();
        var rows = (await conn.QueryAsync<DailyClosing>(
            "SELECT * FROM DailyClosings ORDER BY ClosingDate DESC")).ToList();
        return rows;
    }
}
