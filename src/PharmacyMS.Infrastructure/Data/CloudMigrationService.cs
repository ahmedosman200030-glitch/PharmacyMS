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

    // Wraps an identifier (table or column name) in double quotes so Postgres
    // preserves the exact case that EF Core used when creating the schema.
    // Without this, Postgres lowercases unquoted identifiers (e.g. "Users" -> users)
    // and the query fails with "relation ... does not exist".
    private static string Quote(string identifier) => $"\"{identifier}\"";

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

                // Postgres identifiers must be quoted to preserve PascalCase.
                var quotedTable = Quote(table);
                var quotedColList = string.Join(", ", columns.Select(Quote));

                int count = 0;
                foreach (var row in rows)
                {
                    var rowDict = (IDictionary<string, object>)row;
                    var paramNames = columns.Select(c => "@" + c);
                    var insertSql = $@"INSERT INTO {quotedTable} ({quotedColList})
                                        VALUES ({string.Join(", ", paramNames)})
                                        ON CONFLICT ({Quote("Id")}) DO NOTHING";

                    var dp = new DynamicParameters();
                    foreach (var col in columns)
                        dp.Add("@" + col, rowDict.TryGetValue(col, out var v) ? v : null);

                    await pg.ExecuteAsync(insertSql, dp, tx);
                    count++;
                }

                // pg_get_serial_sequence takes the table/column names as text arguments
                // (not raw identifiers), so the case must be quoted *inside* the string
                // to match exactly, e.g. '"Users"' and 'Id'.
                await pg.ExecuteAsync($@"
                    SELECT setval(
                        pg_get_serial_sequence('{Quote(table)}', 'Id'),
                        COALESCE((SELECT MAX({Quote("Id")}) FROM {quotedTable}), 0) + 1,
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
                    $@"INSERT INTO {Quote("Settings")} ({Quote("Key")}, {Quote("Value")}) VALUES (@Key, @Value)
                      ON CONFLICT ({Quote("Key")}) DO NOTHING",
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
