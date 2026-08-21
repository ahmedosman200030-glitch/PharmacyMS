using System.Data.Common;
using Dapper;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Domain.Entities;
using PharmacyMS.Infrastructure.Data;

namespace PharmacyMS.Infrastructure.Repositories;

public class SaleRepository : ISaleRepository
{
    private readonly AppDbContext _context;
    public SaleRepository(AppDbContext context) => _context = context;

    private async Task AttachItemsAsync(DbConnection conn, List<Sale> sales)
    {
        if (sales.Count == 0) return;
        var ids = sales.Select(s => s.Id).ToList();
        var items = (await conn.QueryAsync<SaleItem>(
            $"SELECT * FROM SaleItems WHERE SaleId IN ({string.Join(",", ids)})")).ToList();
        foreach (var sale in sales)
            sale.Items = items.Where(i => i.SaleId == sale.Id).ToList();
    }

    public async Task<List<Sale>> GetAllAsync()
    {
        using var conn = _context.CreateConnection();
        var sales = (await conn.QueryAsync<Sale>("SELECT * FROM Sales ORDER BY CreatedAt DESC")).ToList();
        var ids = sales.Select(s => s.Id).ToList();
        if (ids.Count > 0)
        {
            var items = (await conn.QueryAsync<SaleItem>(
                $"SELECT * FROM SaleItems WHERE SaleId IN ({string.Join(",", ids)})")).ToList();
            foreach (var sale in sales)
                sale.Items = items.Where(i => i.SaleId == sale.Id).ToList();
        }
        return sales;
    }

    public async Task<List<Sale>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        using var conn = _context.CreateConnection();
        var sales = (await conn.QueryAsync<Sale>(@"
            SELECT * FROM Sales WHERE CreatedAt >= @From AND CreatedAt <= @To ORDER BY CreatedAt DESC",
            new { From = from.ToString("yyyy-MM-dd 00:00:00"), To = to.ToString("yyyy-MM-dd 23:59:59") })).ToList();
        var ids = sales.Select(s => s.Id).ToList();
        if (ids.Count > 0)
        {
            var items = (await conn.QueryAsync<SaleItem>(
                $"SELECT * FROM SaleItems WHERE SaleId IN ({string.Join(",", ids)})")).ToList();
            foreach (var sale in sales)
                sale.Items = items.Where(i => i.SaleId == sale.Id).ToList();
        }
        return sales;
    }

    public async Task<Sale?> GetByInvoiceAsync(string invoiceNumber)
    {
        using var conn = _context.CreateConnection();
        var sale = await conn.QueryFirstOrDefaultAsync<Sale>(
            "SELECT * FROM Sales WHERE InvoiceNumber=@InvoiceNumber", new { InvoiceNumber = invoiceNumber });
        if (sale == null) return null;
        sale.Items = (await conn.QueryAsync<SaleItem>(
            "SELECT * FROM SaleItems WHERE SaleId=@SaleId", new { SaleId = sale.Id })).ToList();
        return sale;
    }

    public async Task<List<Sale>> GetCreditSalesAsync()
    {
        using var conn = _context.CreateConnection();
        var sales = (await conn.QueryAsync<Sale>(
            "SELECT * FROM Sales WHERE AmountPaid < TotalAmount ORDER BY CreatedAt DESC")).ToList();
        var ids = sales.Select(s => s.Id).ToList();
        if (ids.Count > 0)
        {
            var items = (await conn.QueryAsync<SaleItem>(
                $"SELECT * FROM SaleItems WHERE SaleId IN ({string.Join(",", ids)})")).ToList();
            foreach (var sale in sales)
                sale.Items = items.Where(i => i.SaleId == sale.Id).ToList();
        }
        return sales;
    }

    public async Task<int> CreateSaleAsync(Sale sale)
    {
        using var conn = _context.CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();
        var saleId = await conn.ExecuteScalarAsync<int>($@"
            INSERT INTO Sales (InvoiceNumber, CashierId, Subtotal, TaxRate, TaxAmount, TotalAmount,
            CustomerId, CustomerName, PaymentMethod, TotalDiscount, AmountPaid, ChangeDue, CreatedAt)
            VALUES (@InvoiceNumber, @CashierId, @Subtotal, @TaxRate, @TaxAmount, @TotalAmount,
            @CustomerId, @CustomerName, @PaymentMethod, @TotalDiscount, @AmountPaid, @ChangeDue, @CreatedAt)
            {_context.InsertIdSuffix()};", sale, tx);
        foreach (var item in sale.Items)
        {
            await conn.ExecuteAsync(@"
                INSERT INTO SaleItems (SaleId, MedicineId, MedicineName, Unit, UnitPrice, Quantity)
                VALUES (@SaleId, @MedicineId, @MedicineName, @Unit, @UnitPrice, @Quantity)",
                new { SaleId = saleId, item.MedicineId, item.MedicineName, item.Unit, item.UnitPrice, item.Quantity }, tx);

            // Atomic check-and-decrement: only succeeds if enough stock exists at the moment
            // the row lock is acquired. Prevents overselling when multiple PCs sell the same
            // item concurrently against the shared cloud database.
            var rowsAffected = await conn.ExecuteAsync(
                "UPDATE Medicines SET QuantityInStock = QuantityInStock - @Quantity WHERE Id = @MedicineId AND QuantityInStock >= @Quantity",
                new { item.Quantity, item.MedicineId }, tx);

            if (rowsAffected == 0)
            {
                tx.Rollback();
                throw new InvalidOperationException(
                    $"Insufficient stock for \"{item.MedicineName}\" (needed {item.Quantity}). Sale cancelled — someone else may have just sold the remaining stock.");
            }
        }
        tx.Commit();
        sale.Id = saleId;
        return saleId;
    }

    public async Task RecordPaymentAsync(int saleId, decimal amount, string note = "")
    {
        using var conn = _context.CreateConnection();
        var minFn = _context.IsPostgres ? "LEAST" : "MIN";
        await conn.ExecuteAsync($@"
            UPDATE Sales SET AmountPaid = {minFn}(TotalAmount, AmountPaid + @Amount) WHERE Id=@SaleId",
            new { SaleId = saleId, Amount = amount });
        await conn.ExecuteAsync($@"
            INSERT INTO SalePayments (SaleId, Amount, PaidAt, Note)
            VALUES (@SaleId, @Amount, {_context.NowExpr()}, @Note)",
            new { SaleId = saleId, Amount = amount, Note = note });
    }

    public async Task<List<SalePayment>> GetPaymentsAsync(int saleId)
    {
        using var conn = _context.CreateConnection();
        var payments = (await conn.QueryAsync<SalePayment>(
            "SELECT * FROM SalePayments WHERE SaleId=@SaleId ORDER BY PaidAt ASC",
            new { SaleId = saleId })).ToList();
        return payments;
    }

    public async Task<Dictionary<int, DateTime>> GetLastPaymentDatesByCustomerAsync()
    {
        using var conn = _context.CreateConnection();
        var rows = await conn.QueryAsync<CustomerLastPaymentRow>(@"
            SELECT s.CustomerId AS CustomerId, MAX(sp.PaidAt) AS LastPaid
            FROM SalePayments sp
            JOIN Sales s ON sp.SaleId = s.Id
            WHERE s.CustomerId IS NOT NULL
            GROUP BY s.CustomerId");
        return rows.ToDictionary(r => r.CustomerId, r => r.LastPaid);
    }

    private class CustomerLastPaymentRow
    {
        public int CustomerId { get; set; }
        public DateTime LastPaid { get; set; }
    }
}
