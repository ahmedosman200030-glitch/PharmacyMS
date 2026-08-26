using Dapper;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Domain.Entities;
using PharmacyMS.Infrastructure.Data;

namespace PharmacyMS.Infrastructure.Repositories;

public class SaleReturnRepository : ISaleReturnRepository
{
    private readonly AppDbContext _context;
    public SaleReturnRepository(AppDbContext context) => _context = context;

    public async Task<int> CreateAsync(SaleReturn saleReturn)
    {
        using var conn = _context.CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();

        var returnId = await conn.ExecuteScalarAsync<int>($@"
            INSERT INTO SaleReturns (MedicineId, MedicineName, Quantity, UnitPrice, RefundAmount,
            PaymentMethod, Reason, OriginalSaleId, ProcessedByUserId, ProcessedByName, CreatedAt)
            VALUES (@MedicineId, @MedicineName, @Quantity, @UnitPrice, @RefundAmount,
            @PaymentMethod, @Reason, @OriginalSaleId, @ProcessedByUserId, @ProcessedByName, {_context.NowExpr()})
            {_context.InsertIdSuffix()};", saleReturn, tx);

        await conn.ExecuteAsync(
            "UPDATE Medicines SET QuantityInStock = QuantityInStock + @Quantity WHERE Id = @MedicineId",
            new { saleReturn.Quantity, saleReturn.MedicineId }, tx);

        tx.Commit();
        saleReturn.Id = returnId;
        return returnId;
    }

    public async Task<List<SaleReturn>> GetRecentAsync(int limit = 50)
    {
        using var conn = _context.CreateConnection();
        return (await conn.QueryAsync<SaleReturn>(
            "SELECT * FROM SaleReturns ORDER BY CreatedAt DESC LIMIT @Limit", new { Limit = limit })).ToList();
    }

    public async Task<List<SaleReturn>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        using var conn = _context.CreateConnection();
        return (await conn.QueryAsync<SaleReturn>(@"
            SELECT * FROM SaleReturns WHERE CreatedAt >= @From AND CreatedAt <= @To ORDER BY CreatedAt DESC",
            new { From = from.ToString("yyyy-MM-dd 00:00:00"), To = to.ToString("yyyy-MM-dd 23:59:59") })).ToList();
    }

    public async Task<List<SaleReturn>> GetByOriginalSaleIdAsync(int saleId)
    {
        using var conn = _context.CreateConnection();
        return (await conn.QueryAsync<SaleReturn>(
            "SELECT * FROM SaleReturns WHERE OriginalSaleId = @SaleId", new { SaleId = saleId })).ToList();
    }
}
