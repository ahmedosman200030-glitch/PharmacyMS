using Dapper;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Domain.Entities;
using PharmacyMS.Infrastructure.Data;

namespace PharmacyMS.Infrastructure.Repositories;

public class SaleRepository : ISaleRepository
{
    private readonly AppDbContext _context;

    public SaleRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<int> CreateSaleAsync(Sale sale)
    {
        using var conn = _context.CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();

        var saleId = await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO Sales (InvoiceNumber, CashierId, Subtotal, TaxRate, TaxAmount, TotalAmount, CustomerId, CustomerName, PaymentMethod, TotalDiscount, AmountPaid, ChangeDue, CreatedAt)
            VALUES (@InvoiceNumber, @CashierId, @Subtotal, @TaxRate, @TaxAmount, @TotalAmount, @CustomerId, @CustomerName, @PaymentMethod, @TotalDiscount, @AmountPaid, @ChangeDue, datetime('now'));
            SELECT last_insert_rowid();",
            new { sale.InvoiceNumber, sale.CashierId, sale.Subtotal, sale.TaxRate, sale.TaxAmount, sale.TotalAmount,
                  sale.CustomerId, sale.CustomerName, sale.PaymentMethod, sale.TotalDiscount, sale.AmountPaid, sale.ChangeDue }, tx);

        foreach (var item in sale.Items)
        {
            await conn.ExecuteAsync(@"
                INSERT INTO SaleItems (SaleId, MedicineId, MedicineName, UnitPrice, Quantity)
                VALUES (@SaleId, @MedicineId, @MedicineName, @UnitPrice, @Quantity);",
                new { SaleId = saleId, item.MedicineId, item.MedicineName, item.UnitPrice, item.Quantity }, tx);

            await conn.ExecuteAsync(@"
                UPDATE Medicines SET QuantityInStock = QuantityInStock - @Qty WHERE Id = @Id;",
                new { Qty = item.Quantity, Id = item.MedicineId }, tx);
        }

        tx.Commit();
        return saleId;
    }

    public async Task<List<Sale>> GetAllAsync()
    {
        using var conn = _context.CreateConnection();
        var sales = (await conn.QueryAsync<Sale>(
            "SELECT * FROM Sales ORDER BY CreatedAt DESC")).ToList();
        await AttachItemsAsync(conn, sales);
        return sales;
    }

    public async Task<Sale?> GetByInvoiceAsync(string invoiceNumber)
    {
        using var conn = _context.CreateConnection();
        var sale = await conn.QueryFirstOrDefaultAsync<Sale>(
            "SELECT * FROM Sales WHERE InvoiceNumber LIKE @Pattern ORDER BY CreatedAt DESC",
            new { Pattern = $"%{invoiceNumber}%" });
        if (sale == null) return null;
        var sales = new List<Sale> { sale };
        await AttachItemsAsync(conn, sales);
        return sales[0];
    }

    public async Task<List<Sale>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        using var conn = _context.CreateConnection();
        var sales = (await conn.QueryAsync<Sale>(
            "SELECT * FROM Sales WHERE CreatedAt >= @From AND CreatedAt <= @To ORDER BY CreatedAt DESC",
            new { From = from.ToString("yyyy-MM-dd HH:mm:ss"), To = to.ToString("yyyy-MM-dd HH:mm:ss") })).ToList();
        await AttachItemsAsync(conn, sales);
        return sales;
    }

    public async Task<List<Sale>> GetCreditSalesAsync()
    {
        using var conn = _context.CreateConnection();
        var sales = (await conn.QueryAsync<Sale>(
            "SELECT * FROM Sales WHERE TotalAmount > AmountPaid ORDER BY CreatedAt DESC")).ToList();
        await AttachItemsAsync(conn, sales);
        return sales;
    }

    public async Task RecordPaymentAsync(int saleId, decimal amount)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(@"
            UPDATE Sales
            SET AmountPaid = MIN(TotalAmount, AmountPaid + @Amount)
            WHERE Id = @SaleId",
            new { SaleId = saleId, Amount = amount });
    }

    private static async Task AttachItemsAsync(Npgsql.NpgsqlConnection conn, List<Sale> sales)
    {
        if (sales.Count == 0) return;
        var ids = sales.Select(s => s.Id).ToList();
        var items = (await conn.QueryAsync<SaleItem>(
            $"SELECT * FROM SaleItems WHERE SaleId IN ({string.Join(",", ids)})")).ToList();
        var byId = sales.ToDictionary(s => s.Id);
        foreach (var item in items)
        {
            if (byId.TryGetValue(item.SaleId, out var sale))
                sale.Items.Add(item);
        }
    }
}
