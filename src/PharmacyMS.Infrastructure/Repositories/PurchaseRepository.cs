using Dapper;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Domain.Entities;
using PharmacyMS.Infrastructure.Data;

namespace PharmacyMS.Infrastructure.Repositories;

public class PurchaseRepository : IPurchaseRepository
{
    private readonly AppDbContext _context;

    public PurchaseRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<int> CreatePurchaseAsync(Purchase purchase)
    {
        using var conn = _context.CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();

        var purchaseId = await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO Purchases (SupplierId, SupplierName, InvoiceNumber, TotalAmount, CreatedAt)
            VALUES (@SupplierId, @SupplierName, @InvoiceNumber, @TotalAmount, datetime('now'));
            SELECT last_insert_rowid();",
            new { purchase.SupplierId, purchase.SupplierName, purchase.InvoiceNumber, purchase.TotalAmount }, tx);

        foreach (var item in purchase.Items)
        {
            await conn.ExecuteAsync(@"
                INSERT INTO PurchaseItems (PurchaseId, MedicineId, MedicineName, UnitCost, Quantity, BatchNumber, ExpiryDate)
                VALUES (@PurchaseId, @MedicineId, @MedicineName, @UnitCost, @Quantity, @BatchNumber, @ExpiryDate);",
                new
                {
                    PurchaseId = purchaseId,
                    item.MedicineId,
                    item.MedicineName,
                    item.UnitCost,
                    item.Quantity,
                    item.BatchNumber,
                    ExpiryDate = item.ExpiryDate?.ToString("yyyy-MM-dd")
                }, tx);

            // Stock in + push batch/expiry/cost onto the medicine record
            await conn.ExecuteAsync(@"
                UPDATE Medicines SET
                    QuantityInStock = QuantityInStock + @Qty,
                    CostPrice = @UnitCost,
                    BatchNumber = COALESCE(@BatchNumber, BatchNumber),
                    ExpiryDate = COALESCE(@ExpiryDate, ExpiryDate),
                    UpdatedAt = datetime('now')
                WHERE Id = @Id;",
                new
                {
                    Qty = item.Quantity,
                    item.UnitCost,
                    item.BatchNumber,
                    ExpiryDate = item.ExpiryDate?.ToString("yyyy-MM-dd"),
                    Id = item.MedicineId
                }, tx);
        }

        tx.Commit();
        return purchaseId;
    }

    public async Task<IEnumerable<Purchase>> GetAllAsync()
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<Purchase>(
            "SELECT * FROM Purchases ORDER BY CreatedAt DESC");
    }

    public async Task<Purchase?> GetByIdAsync(int id)
    {
        using var conn = _context.CreateConnection();
        var purchase = await conn.QueryFirstOrDefaultAsync<Purchase>(
            "SELECT * FROM Purchases WHERE Id = @Id", new { Id = id });
        if (purchase == null) return null;

        var items = await conn.QueryAsync<PurchaseItem>(
            "SELECT * FROM PurchaseItems WHERE PurchaseId = @Id", new { Id = id });
        purchase.Items = items.ToList();
        return purchase;
    }

    public async Task<IEnumerable<SupplierSpend>> GetSpendBySupplierAsync()
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<SupplierSpend>(@"
            SELECT SupplierName, SUM(TotalAmount) AS Total
            FROM Purchases
            GROUP BY SupplierName
            ORDER BY Total DESC;");
    }

    public async Task<IEnumerable<MedicineSpend>> GetSpendByMedicineAsync()
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<MedicineSpend>(@"
            SELECT MedicineName, SUM(UnitCost * Quantity) AS Total
            FROM PurchaseItems
            GROUP BY MedicineName
            ORDER BY Total DESC;");
    }

    public async Task<decimal> GetTotalSpendAsync()
    {
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<decimal>(
            "SELECT COALESCE(SUM(TotalAmount), 0) FROM Purchases;");
    }

    public async Task RecordPaymentAsync(int purchaseId, decimal amount)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(@"
            UPDATE Purchases
            SET AmountPaid = MIN(TotalAmount, AmountPaid + @Amount)
            WHERE Id = @Id;",
            new { Amount = amount, Id = purchaseId });
    }
}
