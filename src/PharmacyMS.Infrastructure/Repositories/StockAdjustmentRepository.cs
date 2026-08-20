using Dapper;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Domain.Entities;
using PharmacyMS.Infrastructure.Data;

namespace PharmacyMS.Infrastructure.Repositories;

public class StockAdjustmentRepository : IStockAdjustmentRepository
{
    private readonly AppDbContext _context;
    public StockAdjustmentRepository(AppDbContext context) => _context = context;

    public async Task<IEnumerable<StockAdjustment>> GetRecentAsync(int limit = 50)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<StockAdjustment>(
            "SELECT * FROM StockAdjustments ORDER BY CreatedAt DESC LIMIT @Limit", new { Limit = limit });
    }

    public async Task<int> CreateAsync(StockAdjustment adjustment)
    {
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<int>($@"
            INSERT INTO StockAdjustments (MedicineId, MedicineName, QuantityChange, Reason,
            AdjustedByUserId, AdjustedByName, CreatedAt)
            VALUES (@MedicineId, @MedicineName, @QuantityChange, @Reason,
            @AdjustedByUserId, @AdjustedByName, {_context.NowExpr()})
            {_context.InsertIdSuffix()};", adjustment);
    }
}
