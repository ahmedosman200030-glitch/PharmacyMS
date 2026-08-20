using Dapper;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Domain.Entities;
using PharmacyMS.Domain.Enums;
using PharmacyMS.Infrastructure.Data;

namespace PharmacyMS.Infrastructure.Repositories;

public class GoodsReceiptRepository : IGoodsReceiptRepository
{
    private readonly AppDbContext _context;
    public GoodsReceiptRepository(AppDbContext context) => _context = context;

    // Records the receipt only. Stock, PO fulfillment, and PO status are NOT
    // touched here — they only take effect once an admin approves (ApproveAsync).
    public async Task<int> ReceiveAsync(GoodsReceipt receipt)
    {
        using var conn = _context.CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();

        var receiptId = await conn.ExecuteScalarAsync<int>($@"
            INSERT INTO GoodsReceipts (PurchaseOrderId, ReceivedAt, ReceivedByUserId, Notes, ApprovalStatus)
            VALUES (@PurchaseOrderId, {_context.NowExpr()}, @ReceivedByUserId, @Notes, @ApprovalStatus)
            {_context.InsertIdSuffix()};",
            new
            {
                receipt.PurchaseOrderId,
                receipt.ReceivedByUserId,
                receipt.Notes,
                ApprovalStatus = (int)ApprovalStatus.Pending
            }, tx);

        foreach (var item in receipt.Items)
        {
            await conn.ExecuteAsync(@"
                INSERT INTO GoodsReceiptItems
                    (GoodsReceiptId, PurchaseOrderItemId, MedicineId, MedicineName, OrderedQuantity, ReceivedQuantity, BatchNumber, ExpiryDate, UnitCost)
                VALUES
                    (@GoodsReceiptId, @PurchaseOrderItemId, @MedicineId, @MedicineName, @OrderedQuantity, @ReceivedQuantity, @BatchNumber, @ExpiryDate, @UnitCost)",
                new
                {
                    GoodsReceiptId = receiptId,
                    item.PurchaseOrderItemId,
                    item.MedicineId,
                    item.MedicineName,
                    item.OrderedQuantity,
                    item.ReceivedQuantity,
                    item.BatchNumber,
                    item.ExpiryDate,
                    item.UnitCost
                }, tx);
        }

        tx.Commit();
        return receiptId;
    }

    // Applies the stock increase, PO item fulfillment, and PO status recompute
    // that used to happen immediately in ReceiveAsync — now deferred to approval.
    public async Task ApproveAsync(int receiptId)
    {
        using var conn = _context.CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();

        var receipt = await conn.QueryFirstOrDefaultAsync<GoodsReceipt>(
            "SELECT * FROM GoodsReceipts WHERE Id=@Id", new { Id = receiptId }, tx);
        if (receipt == null) { tx.Rollback(); return; }

        var items = (await conn.QueryAsync<GoodsReceiptItem>(
            "SELECT * FROM GoodsReceiptItems WHERE GoodsReceiptId=@Id", new { Id = receiptId }, tx)).ToList();

        foreach (var item in items)
        {
            await conn.ExecuteAsync(
                "UPDATE Medicines SET QuantityInStock = QuantityInStock + @Qty WHERE Id = @MedicineId",
                new { Qty = item.ReceivedQuantity, item.MedicineId }, tx);

            await conn.ExecuteAsync(
                "UPDATE PurchaseOrderItems SET ReceivedQuantity = ReceivedQuantity + @Qty WHERE Id = @Id",
                new { Qty = item.ReceivedQuantity, Id = item.PurchaseOrderItemId }, tx);
        }

        var lineStatus = (await conn.QueryAsync<(int Quantity, int ReceivedQuantity)>(
            "SELECT Quantity, ReceivedQuantity FROM PurchaseOrderItems WHERE PurchaseOrderId=@Id",
            new { Id = receipt.PurchaseOrderId }, tx)).ToList();

        var newStatus = lineStatus.All(l => l.ReceivedQuantity >= l.Quantity)
            ? PurchaseOrderStatus.Received
            : lineStatus.Any(l => l.ReceivedQuantity > 0)
                ? PurchaseOrderStatus.PartiallyReceived
                : PurchaseOrderStatus.Sent;

        await conn.ExecuteAsync("UPDATE PurchaseOrders SET Status=@Status WHERE Id=@Id",
            new { Id = receipt.PurchaseOrderId, Status = (int)newStatus }, tx);

        await conn.ExecuteAsync("UPDATE GoodsReceipts SET ApprovalStatus=@Status WHERE Id=@Id",
            new { Id = receiptId, Status = (int)ApprovalStatus.Approved }, tx);

        tx.Commit();
    }

    public async Task RejectAsync(int receiptId, string reason)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync("UPDATE GoodsReceipts SET ApprovalStatus=@Status, RejectionReason=@Reason WHERE Id=@Id",
            new { Id = receiptId, Status = (int)ApprovalStatus.Rejected, Reason = reason });
    }

    public async Task<IEnumerable<GoodsReceipt>> GetAllAsync()
    {
        using var conn = _context.CreateConnection();
        var receipts = (await conn.QueryAsync<GoodsReceipt>(@"
            SELECT gr.*, u.FullName AS ReceivedByUserName
            FROM GoodsReceipts gr
            LEFT JOIN Users u ON u.Id = gr.ReceivedByUserId
            ORDER BY gr.ReceivedAt DESC")).ToList();
        await AttachItemsAsync(conn, receipts);
        return receipts;
    }

    public async Task<GoodsReceipt?> GetByIdAsync(int id)
    {
        using var conn = _context.CreateConnection();
        var receipt = await conn.QueryFirstOrDefaultAsync<GoodsReceipt>(@"
            SELECT gr.*, u.FullName AS ReceivedByUserName
            FROM GoodsReceipts gr
            LEFT JOIN Users u ON u.Id = gr.ReceivedByUserId
            WHERE gr.Id=@Id", new { Id = id });
        if (receipt == null) return null;
        receipt.Items = (await conn.QueryAsync<GoodsReceiptItem>(
            "SELECT * FROM GoodsReceiptItems WHERE GoodsReceiptId=@Id", new { Id = id })).ToList();
        return receipt;
    }

    public async Task<GoodsReceipt?> GetByIdWithOrderNumberAsync(int id) => await GetByIdAsync(id);

    private static async Task AttachItemsAsync(System.Data.IDbConnection conn, List<GoodsReceipt> receipts)
    {
        var ids = receipts.Select(r => r.Id).ToList();
        if (ids.Count == 0) return;
        var items = (await conn.QueryAsync<GoodsReceiptItem>(
            $"SELECT * FROM GoodsReceiptItems WHERE GoodsReceiptId IN ({string.Join(",", ids)})")).ToList();
        foreach (var r in receipts)
            r.Items = items.Where(i => i.GoodsReceiptId == r.Id).ToList();
    }
}
