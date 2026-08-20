using Dapper;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Domain.Entities;
using PharmacyMS.Infrastructure.Data;

namespace PharmacyMS.Infrastructure.Repositories;

public class DailyClosingRepository : IDailyClosingRepository
{
    private readonly AppDbContext _context;
    public DailyClosingRepository(AppDbContext context) => _context = context;

    public async Task<List<DailyClosing>> GetHistoryAsync()
    {
        using var conn = _context.CreateConnection();
        return (await conn.QueryAsync<DailyClosing>(
            "SELECT * FROM DailyClosings ORDER BY CreatedAt DESC")).ToList();
    }

    public async Task<bool> HasClosedTodayAsync()
    {
        using var conn = _context.CreateConnection();
        var sql = _context.IsPostgres
            ? "SELECT COUNT(*) FROM DailyClosings WHERE ClosingDate::date = CURRENT_DATE"
            : "SELECT COUNT(*) FROM DailyClosings WHERE date(ClosingDate)=date('now')";
        var count = await conn.ExecuteScalarAsync<int>(sql);
        return count > 0;
    }

    public async Task<int> CreateAsync(DailyClosing closing)
    {
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<int>($@"
            INSERT INTO DailyClosings (ClosingDate, CashSales, CardSales, MobileSales, InsuranceSales,
            ExpectedCash, ActualCash, Difference, Notes, ClosedByUserId, ClosedByUserName, CreatedAt)
            VALUES (@ClosingDate, @CashSales, @CardSales, @MobileSales, @InsuranceSales,
            @ExpectedCash, @ActualCash, @Difference, @Notes, @ClosedByUserId, @ClosedByUserName, {_context.NowExpr()})
            {_context.InsertIdSuffix()};", closing);
    }
}
