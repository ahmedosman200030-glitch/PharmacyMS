using Dapper;
using Microsoft.Data.Sqlite;
using Npgsql;

namespace PharmacyMS.Infrastructure.Data;

public class CloudMigrationService
{
    // Order matters: parents before children.
    private static readonly (string Table, string[] Columns)[] TablesInOrder = new[]
    {
        ("Users", new[] { "Id","Username","PasswordHash","FullName","Role","IsActive","CreatedAt","UpdatedAt","LastLogin","SecurityQuestion","SecurityAnswerHash","AvatarPath","Permissions" }),
        ("Categories", new[] { "Id","Name","Description","IsActive","CreatedAt" }),
        ("Suppliers", new[] { "Id","Name","ContactPerson","Phone","Email","Address","IsActive","CreatedAt","ApprovalStatus" }),
        ("Customers", new[] { "Id","Name","Phone","Email","Address","IsActive","CreatedAt","UpdatedAt","ApprovalStatus" }),
        ("Medicines", new[] { "Id","Name","GenericName","Category","Manufacturer","UnitPrice","QuantityInStock","ReorderLevel","ExpiryDate","BatchNumber","IsActive","CreatedAt","UpdatedAt","Barcode","Supplier","CostPrice" }),
        ("Purchases", new[] { "Id","SupplierName","InvoiceNumber","TotalAmount","CreatedAt","SupplierId","AmountPaid","ApprovalStatus" }),
        ("PurchaseItems", new[] { "Id","PurchaseId","MedicineId","MedicineName","UnitCost","Quantity","BatchNumber","ExpiryDate" }),
        ("Sales", new[] { "Id","InvoiceNumber","CashierId","TotalAmount","CreatedAt","Subtotal","TaxRate","TaxAmount","CustomerId","AmountPaid","CustomerName","PaymentMethod","TotalDiscount","ChangeDue" }),
        ("SaleItems", new[] { "Id","SaleId","MedicineId","MedicineName","UnitPrice","Quantity" }),
        ("SalePayments", new[] { "Id","SaleId","Amount","PaidAt","Note" }),
        ("StockAdjustments", new[] { "Id","MedicineId","MedicineName","QuantityChange","Reason","AdjustedByUserId","AdjustedByName","CreatedAt" }),
        ("SaleReturns", new[] { "Id","MedicineId","MedicineName","Quantity","UnitPrice","RefundAmount","PaymentMethod","Reason","OriginalSaleId","ProcessedByUserId","ProcessedByName","CreatedAt" }),
        ("DailyClosings", new[] { "Id","ClosingDate","CashSales","CardSales","MobileSales","InsuranceSales","ExpectedCash","ActualCash","Difference","Notes","ClosedByUserId","ClosedByUserName","CreatedAt" }),
        ("Expenses", new[] { "Id","Date","Category","Description","Amount","CreatedBy","CreatedAt" }),
    };

    public class MigrationProgress
    {
        public string Table { get; set; } = "";
        public int RowsCopied { get; set; }
    }

    // Wraps a TABLE name in double quotes so Postgres preserves the exact
    // PascalCase used elsewhere in the schema (e.g. "Users", "Sales").
    // Column names are intentionally left unquoted, matching the convention
    // already used in DatabaseInitializer.cs (see the OtherIncomes/Expenses
    // ALTER TABLE statements there).
    private static string QuoteTable(string tableName) => $"\"{tableName}\"";

    public async Task<List<MigrationProgress>> MigrateAsync(
        string postgresConnectionString,
        IProgress<MigrationProgress>? progress = null)
    {
        var results = new List<MigrationProgress>();

        var sqliteConnString = AppDbContext.DefaultSqliteConnectionString();
        using var sqlite = new SqliteConnection(sqliteConnString);
        await sqlite.OpenAsync();

        using var pg = new NpgsqlConnection(postgresConnectionString);
        await pg.OpenAsync();
        using var tx = await pg.BeginTransactionAsync();

        try
        {
            foreach (var (table, columns) in TablesInOrder)
            {
                // SQLite is case-insensitive by default, so unquoted names are fine here.
                var colList = string.Join(", ", columns);
                var rows = (await sqlite.QueryAsync(
                    $"SELECT {colList} FROM {table}")).ToList();

                // Only the table name needs quoting for Postgres; columns stay unquoted.
                var quotedTable = QuoteTable(table);

                int count = 0;
                foreach (var row in rows)
                {
                    var rowDict = (IDictionary<string, object>)row;
                    var paramNames = columns.Select(c => "@" + c);
                    var insertSql = $@"INSERT INTO {quotedTable} ({colList})
                                        VALUES ({string.Join(", ", paramNames)})
                                        ON CONFLICT (Id) DO NOTHING";

                    var dp = new DynamicParameters();
                    foreach (var col in columns)
                        dp.Add("@" + col, rowDict.TryGetValue(col, out var v) ? v : null);

                    await pg.ExecuteAsync(insertSql, dp, tx);
                    count++;
                }

                // pg_get_serial_sequence takes the table name as a text argument
                // (not a raw identifier), so the case must be quoted *inside* the
                // string to match, e.g. '"Users"'. The column name stays unquoted.
                await pg.ExecuteAsync($@"
                    SELECT setval(
                        pg_get_serial_sequence('{quotedTable}', 'id'),
                        COALESCE((SELECT MAX(Id) FROM {quotedTable}), 0) + 1,
                        false)", transaction: tx);

                var p = new MigrationProgress { Table = table, RowsCopied = count };
                results.Add(p);
                progress?.Report(p);
            }

            var settingsRows = (await sqlite.QueryAsync(
                "SELECT [Key], Value FROM Settings")).ToList();
            int settingsCount = 0;
            foreach (var row in settingsRows)
            {
                var rowDict = (IDictionary<string, object>)row;
                await pg.ExecuteAsync(
                    $@"INSERT INTO {QuoteTable("Settings")} (Key, Value) VALUES (@Key, @Value)
                      ON CONFLICT (Key) DO NOTHING",
                    new { Key = rowDict["Key"], Value = rowDict["Value"] }, tx);
                settingsCount++;
            }
            var sp = new MigrationProgress { Table = "Settings", RowsCopied = settingsCount };
            results.Add(sp);
            progress?.Report(sp);

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        return results;
    }
}
