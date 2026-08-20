using Dapper;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Domain.Entities;
using PharmacyMS.Domain.Enums;
using PharmacyMS.Infrastructure.Data;

namespace PharmacyMS.Infrastructure.Repositories;

public class PurchaseRepository : IPurchaseRepository
{
    private readonly AppDbContext _context;
    public PurchaseRepository(AppDbContext context) => _context = context;

    public async Task<IEnumerable<Purchase>> GetAllAsync()
    {
        using var conn = _context.CreateConnection();
        var purchases = (await conn.QueryAsync<Purchase>("SELECT * FROM Purchases ORDER BY CreatedAt DESC")).ToList();
        var ids = purchases.Select(p => p.Id).ToList();
        if (ids.Count > 0)
        {
            var items = (await conn.QueryAsync<PurchaseItem>(
                $"SELECT * FROM PurchaseItems WHERE PurchaseId IN ({string.Join(",", ids)})")).ToList();
            foreach (var p in purchases)
                p.Items = items.Where(i => i.PurchaseId == p.Id).ToList();
        }
        return purchases;
    }

    public async Task<Purchase?> GetByIdAsync(int id)
    {
        using var conn = _context.CreateConnection();
        var purchase = await conn.QueryFirstOrDefaultAsync<Purchase>(
            "SELECT * FROM Purchases WHERE Id=@Id", new { Id = id });
        if (purchase == null) return null;
        purchase.Items = (await conn.QueryAsync<PurchaseItem>(
            "SELECT * FROM PurchaseItems WHERE PurchaseId=@Id", new { Id = id })).ToList();
        return purchase;
    }

    public async Task<int> CreatePurchaseAsync(Purchase purchase)
    {
        using var conn = _context.CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();
        var purchaseId = await conn.ExecuteScalarAsync<int>($@"
            INSERT INTO Purchases (SupplierId, SupplierName, InvoiceNumber, TotalAmount, AmountPaid, CreatedAt, ApprovalStatus)
            VALUES (@SupplierId, @SupplierName, @InvoiceNumber, @TotalAmount, @AmountPaid, {_context.NowExpr()}, @ApprovalStatus)
            {_context.InsertIdSuffix()};", purchase, tx);
        foreach (var item in purchase.Items)
        {
            await conn.ExecuteAsync(@"
                INSERT INTO PurchaseItems (PurchaseId, MedicineId, MedicineName, UnitCost, Quantity, BatchNumber, ExpiryDate)
                VALUES (@PurchaseId, @MedicineId, @MedicineName, @UnitCost, @Quantity, @BatchNumber, @ExpiryDate)",
                new { PurchaseId = purchaseId, item.MedicineId, item.MedicineName, item.UnitCost, item.Quantity, item.BatchNumber, item.ExpiryDate }, tx);
            await conn.ExecuteAsync(
                "UPDATE Medicines SET QuantityInStock = QuantityInStock + @Quantity WHERE Id = @MedicineId",
                new { item.Quantity, item.MedicineId }, tx);
        }
        tx.Commit();
        return purchaseId;
    }

    public async Task<int> CreateBillFromReceiptAsync(int goodsReceiptId, string? invoiceNumber)
    {
        using var conn = _context.CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();

        var receipt = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT * FROM GoodsReceipts WHERE Id=@Id", new { Id = goodsReceiptId }, tx);
        if (receipt == null) throw new InvalidOperationException("Goods receipt not found.");

        var order = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT * FROM PurchaseOrders WHERE Id=@Id", new { Id = (int)receipt.PurchaseOrderId }, tx);
        if (order == null) throw new InvalidOperationException("Purchase order not found.");

        var receiptItems = (await conn.QueryAsync<Domain.Entities.GoodsReceiptItem>(
            "SELECT * FROM GoodsReceiptItems WHERE GoodsReceiptId=@Id", new { Id = goodsReceiptId }, tx)).ToList();
        var total = receiptItems.Sum(i => i.LineTotal);

        var purchaseId = await conn.ExecuteScalarAsync<int>($@"
            INSERT INTO Purchases (SupplierId, SupplierName, InvoiceNumber, TotalAmount, AmountPaid, CreatedAt, ApprovalStatus, PurchaseOrderId, GoodsReceiptId)
            VALUES (@SupplierId, @SupplierName, @InvoiceNumber, @TotalAmount, 0, {_context.NowExpr()}, @ApprovalStatus, @PurchaseOrderId, @GoodsReceiptId)
            {_context.InsertIdSuffix()};",
            new
            {
                SupplierId = (int?)order.SupplierId,
                SupplierName = (string)order.SupplierName,
                InvoiceNumber = invoiceNumber,
                TotalAmount = total,
                ApprovalStatus = 1,
                PurchaseOrderId = (int)order.Id,
                GoodsReceiptId = goodsReceiptId
            }, tx);

        // Bill line items are recorded for reporting only — stock was already
        // updated when the goods were received, so no Medicines update here.
        foreach (var item in receiptItems)
        {
            await conn.ExecuteAsync(@"
                INSERT INTO PurchaseItems (PurchaseId, MedicineId, MedicineName, UnitCost, Quantity, BatchNumber, ExpiryDate)
                VALUES (@PurchaseId, @MedicineId, @MedicineName, @UnitCost, @Quantity, @BatchNumber, @ExpiryDate)",
                new
                {
                    PurchaseId = purchaseId,
                    item.MedicineId,
                    item.MedicineName,
                    item.UnitCost,
                    Quantity = item.ReceivedQuantity,
                    item.BatchNumber,
                    item.ExpiryDate
                }, tx);
        }

        tx.Commit();
        return purchaseId;
    }

    public async Task<IEnumerable<SupplierSpend>> GetSpendBySupplierAsync()
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<SupplierSpend>(@"
            SELECT SupplierName, COALESCE(SUM(TotalAmount),0) AS Total FROM Purchases GROUP BY SupplierName");
    }

    public async Task<IEnumerable<MedicineSpend>> GetSpendByMedicineAsync()
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<MedicineSpend>(@"
            SELECT MedicineName, COALESCE(SUM(UnitCost*Quantity),0) AS Total FROM PurchaseItems GROUP BY MedicineName");
    }

    public async Task<decimal> GetTotalSpendAsync()
    {
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<decimal>("SELECT COALESCE(SUM(TotalAmount),0) FROM Purchases");
    }

    public async Task RecordPaymentAsync(int purchaseId, decimal amount, string note = "")
    {
        using var conn = _context.CreateConnection();
        var minFn = _context.IsPostgres ? "LEAST" : "MIN";
        await conn.ExecuteAsync($@"
            UPDATE Purchases SET AmountPaid = {minFn}(TotalAmount, AmountPaid + @Amount) WHERE Id=@PurchaseId",
            new { PurchaseId = purchaseId, Amount = amount });
        await conn.ExecuteAsync($@"
            INSERT INTO PurchasePayments (PurchaseId, Amount, PaidAt, Note)
            VALUES (@PurchaseId, @Amount, {_context.NowExpr()}, @Note)",
            new { PurchaseId = purchaseId, Amount = amount, Note = note });
    }

    public async Task<List<PurchasePayment>> GetPaymentsAsync(int purchaseId)
    {
        using var conn = _context.CreateConnection();
        var payments = (await conn.QueryAsync<PurchasePayment>(
            "SELECT * FROM PurchasePayments WHERE PurchaseId=@PurchaseId ORDER BY PaidAt ASC",
            new { PurchaseId = purchaseId })).ToList();
        return payments;
    }

    public async Task<Dictionary<int, DateTime>> GetLastPaymentDatesBySupplierAsync()
    {
        using var conn = _context.CreateConnection();
        var rows = await conn.QueryAsync<SupplierLastPaymentRow>(@"
            SELECT pu.SupplierId AS SupplierId, MAX(pp.PaidAt) AS LastPaid
            FROM PurchasePayments pp
            JOIN Purchases pu ON pp.PurchaseId = pu.Id
            WHERE pu.SupplierId IS NOT NULL
            GROUP BY pu.SupplierId");
        return rows.ToDictionary(r => r.SupplierId, r => r.LastPaid);
    }

    public async Task UpdateApprovalStatusAsync(int purchaseId, ApprovalStatus status)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync("UPDATE Purchases SET ApprovalStatus=@Status WHERE Id=@Id",
            new { Id = purchaseId, Status = status });
    }

    private class SupplierLastPaymentRow
    {
        public int SupplierId { get; set; }
        public DateTime LastPaid { get; set; }
    }
}
