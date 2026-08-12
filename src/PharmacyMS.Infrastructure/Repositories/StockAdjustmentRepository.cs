using Dapper;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Domain.Entities;
using PharmacyMS.Infrastructure.Data;

namespace PharmacyMS.Infrastructure.Repositories;

public class StockAdjustmentRepository : IStockAdjustmentRepository
{
    private readonly AppDbContext _context;

    public StockAdjustmentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<int> CreateAsync(StockAdjustment adjustment)
    {
        using var conn = _context.CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();

        var currentStock = await conn.ExecuteScalarAsync<int>(
            "SELECT QuantityInStock FROM Medicines WHERE Id = @Id",
            new { Id = adjustment.MedicineId }, tx);

        var newStock = currentStock + adjustment.QuantityChange;
        if (newStock < 0)
        {
            tx.Rollback();
            throw new InvalidOperationException(
                $"Adjustment would result in negative stock ({newStock}). Current stock is {currentStock}.");
        }

        var id = await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO StockAdjustments (MedicineId, MedicineName, QuantityChange, Reason, AdjustedByUserId, AdjustedByName, CreatedAt)
            VALUES (@MedicineId, @MedicineName, @QuantityChange, @Reason, @AdjustedByUserId, @AdjustedByName, datetime('now'));
            SELECT last_insert_rowid();",
            adjustment, tx);

        await conn.ExecuteAsync(
            "UPDATE Medicines SET QuantityInStock = @NewStock, UpdatedAt = datetime('now') WHERE Id = @Id",
            new { NewStock = newStock, Id = adjustment.MedicineId }, tx);

        tx.Commit();
        return id;
    }

    public async Task<IEnumerable<StockAdjustment>> GetRecentAsync(int limit = 50)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<StockAdjustment>(
            "SELECT * FROM StockAdjustments ORDER BY CreatedAt DESC LIMIT @Limit",
            new { Limit = limit });
    }
}
