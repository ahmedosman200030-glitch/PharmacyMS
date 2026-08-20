using Dapper;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Domain.Entities;
using PharmacyMS.Domain.Enums;
using PharmacyMS.Infrastructure.Data;

namespace PharmacyMS.Infrastructure.Repositories;

public class PurchaseOrderRepository : IPurchaseOrderRepository
{
    private readonly AppDbContext _context;
    public PurchaseOrderRepository(AppDbContext context) => _context = context;

    public async Task<int> CreateAsync(PurchaseOrder order)
    {
        using var conn = _context.CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();

        var orderId = await conn.ExecuteScalarAsync<int>($@"
            INSERT INTO PurchaseOrders (SupplierId, SupplierName, OrderNumber, Status, ExpectedDate, Notes, CreatedAt, CreatedByUserId)
            VALUES (@SupplierId, @SupplierName, @OrderNumber, @Status, @ExpectedDate, @Notes, {_context.NowExpr()}, @CreatedByUserId)
            {_context.InsertIdSuffix()};", order, tx);

        foreach (var item in order.Items)
        {
            await conn.ExecuteAsync(@"
                INSERT INTO PurchaseOrderItems (PurchaseOrderId, MedicineId, MedicineName, Quantity, Unit, UnitCost, ReceivedQuantity)
                VALUES (@PurchaseOrderId, @MedicineId, @MedicineName, @Quantity, @Unit, @UnitCost, 0)",
                new { PurchaseOrderId = orderId, item.MedicineId, item.MedicineName, item.Quantity, item.Unit, item.UnitCost }, tx);
        }

        tx.Commit();
        return orderId;
    }

    public async Task<IEnumerable<PurchaseOrder>> GetAllAsync()
    {
        using var conn = _context.CreateConnection();
        var orders = (await conn.QueryAsync<PurchaseOrder>(
            "SELECT * FROM PurchaseOrders ORDER BY CreatedAt DESC")).ToList();
        await AttachItemsAsync(conn, orders);
        return orders;
    }

    public async Task<PurchaseOrder?> GetByIdAsync(int id)
    {
        using var conn = _context.CreateConnection();
        var order = await conn.QueryFirstOrDefaultAsync<PurchaseOrder>(
            "SELECT * FROM PurchaseOrders WHERE Id=@Id", new { Id = id });
        if (order == null) return null;
        order.Items = (await conn.QueryAsync<PurchaseOrderItem>(
            "SELECT * FROM PurchaseOrderItems WHERE PurchaseOrderId=@Id", new { Id = id })).ToList();
        return order;
    }

    public async Task<IEnumerable<PurchaseOrder>> GetPendingReceivingAsync()
    {
        using var conn = _context.CreateConnection();
        var orders = (await conn.QueryAsync<PurchaseOrder>(@"
            SELECT * FROM PurchaseOrders
            WHERE Status IN (@Sent, @Partial)
              AND Id NOT IN (SELECT PurchaseOrderId FROM GoodsReceipts WHERE ApprovalStatus = @Pending)
            ORDER BY CreatedAt DESC",
            new { Sent = (int)PurchaseOrderStatus.Sent, Partial = (int)PurchaseOrderStatus.PartiallyReceived, Pending = (int)ApprovalStatus.Pending })).ToList();
        await AttachItemsAsync(conn, orders);
        return orders;
    }

    public async Task UpdateStatusAsync(int id, PurchaseOrderStatus status)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync("UPDATE PurchaseOrders SET Status=@Status WHERE Id=@Id",
            new { Id = id, Status = (int)status });
    }

    private static async Task AttachItemsAsync(System.Data.IDbConnection conn, List<PurchaseOrder> orders)
    {
        var ids = orders.Select(o => o.Id).ToList();
        if (ids.Count == 0) return;
        var items = (await conn.QueryAsync<PurchaseOrderItem>(
            $"SELECT * FROM PurchaseOrderItems WHERE PurchaseOrderId IN ({string.Join(",", ids)})")).ToList();
        foreach (var o in orders)
            o.Items = items.Where(i => i.PurchaseOrderId == o.Id).ToList();
    }
}
