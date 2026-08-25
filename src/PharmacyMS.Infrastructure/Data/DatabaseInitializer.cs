using Dapper;
using PharmacyMS.Domain.Enums;

namespace PharmacyMS.Infrastructure.Data;

public class DatabaseInitializer
{
    private readonly AppDbContext _context;

    public DatabaseInitializer(AppDbContext context)
    {
        _context = context;
    }

    // Wraps a TABLE name in double quotes when running against Postgres, so
    // CREATE TABLE preserves PascalCase (e.g. "Users") instead of Postgres
    // silently folding an unquoted name to lowercase ("users"). SQLite table
    // names are left unquoted/unchanged, since SQLite is case-insensitive and
    // this avoids touching existing SQLite behavior. Column names inside each
    // CREATE TABLE are intentionally left unquoted on both providers — this
    // matches the existing convention already used below (see the
    // ALTER TABLE "OtherIncomes" / "Expenses" statements, which quote only
    // the table name and leave columns like PaymentMethod unquoted).
    private string T(string tableName) => _context.IsPostgres ? $"\"{tableName}\"" : tableName;

    public async Task InitializeAsync()
    {
        using var conn = _context.CreateConnection();
        await conn.OpenAsync();

        var pk = _context.AutoIncrementPk();
        var now = _context.NowExpr();

        await conn.ExecuteAsync($@"
            CREATE TABLE IF NOT EXISTS {T("Users")} (
                Id {pk},
                Username TEXT NOT NULL UNIQUE,
                PasswordHash TEXT NOT NULL,
                FullName TEXT NOT NULL,
                Role INTEGER NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT,
                LastLogin TEXT,
                SecurityQuestion TEXT,
                SecurityAnswerHash TEXT,
                AvatarPath TEXT,
                Permissions INTEGER NOT NULL DEFAULT 0
            );");

        await conn.ExecuteAsync($@"
            CREATE TABLE IF NOT EXISTS {T("Medicines")} (
                Id {pk},
                Name TEXT NOT NULL,
                GenericName TEXT,
                Category TEXT,
                Manufacturer TEXT,
                UnitPrice REAL NOT NULL DEFAULT 0,
                QuantityInStock INTEGER NOT NULL DEFAULT 0,
                ReorderLevel INTEGER NOT NULL DEFAULT 10,
                ExpiryDate TEXT,
                BatchNumber TEXT,
                IsActive INTEGER NOT NULL DEFAULT 1,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT,
                Barcode TEXT,
                Supplier TEXT,
                CostPrice REAL NOT NULL DEFAULT 0,
                Unit TEXT NOT NULL DEFAULT 'Box'
            );");

        
        // Migration: add Unit column to Medicines if missing
        try
        {
            await conn.ExecuteAsync($"ALTER TABLE {T("Medicines")} ADD COLUMN Unit TEXT NOT NULL DEFAULT 'Box'");
        }
        catch { /* column already exists */ }

        await conn.ExecuteAsync($@"
            CREATE TABLE IF NOT EXISTS {T("Sales")} (
                Id {pk},
                InvoiceNumber TEXT NOT NULL,
                CashierId INTEGER NOT NULL,
                TotalAmount REAL NOT NULL,
                CreatedAt TEXT NOT NULL,
                Subtotal REAL NOT NULL DEFAULT 0,
                TaxRate REAL NOT NULL DEFAULT 0,
                TaxAmount REAL NOT NULL DEFAULT 0,
                CustomerId INTEGER,
                AmountPaid REAL NOT NULL DEFAULT 0,
                CustomerName TEXT NOT NULL DEFAULT 'Walk-in Customer',
                PaymentMethod TEXT NOT NULL DEFAULT 'Cash',
                TotalDiscount REAL NOT NULL DEFAULT 0,
                ChangeDue REAL NOT NULL DEFAULT 0
            );");

        await conn.ExecuteAsync($@"
            CREATE TABLE IF NOT EXISTS {T("SaleItems")} (
                Id {pk},
                SaleId INTEGER NOT NULL,
                MedicineId INTEGER NOT NULL,
                MedicineName TEXT NOT NULL,
                Unit TEXT NOT NULL DEFAULT 'Box',
                UnitPrice REAL NOT NULL,
                Quantity INTEGER NOT NULL
            );");

        // Migration: add Unit column to SaleItems if missing
        try
        {
            await conn.ExecuteAsync($"ALTER TABLE {T("SaleItems")} ADD COLUMN Unit TEXT NOT NULL DEFAULT 'Box'");
        }
        catch { /* column already exists */ }

        await conn.ExecuteAsync($@"
            CREATE TABLE IF NOT EXISTS {T("Purchases")} (
                Id {pk},
                SupplierName TEXT NOT NULL,
                InvoiceNumber TEXT,
                TotalAmount REAL NOT NULL,
                CreatedAt TEXT NOT NULL,
                SupplierId INTEGER,
                AmountPaid REAL NOT NULL DEFAULT 0,
                ApprovalStatus INTEGER NOT NULL DEFAULT 0,
                PurchaseOrderId INTEGER,
                GoodsReceiptId INTEGER
            );");

        await conn.ExecuteAsync($@"
            CREATE TABLE IF NOT EXISTS {T("PurchaseItems")} (
                Id {pk},
                PurchaseId INTEGER NOT NULL,
                MedicineId INTEGER NOT NULL,
                MedicineName TEXT NOT NULL,
                UnitCost REAL NOT NULL,
                Quantity INTEGER NOT NULL,
                BatchNumber TEXT,
                ExpiryDate TEXT
            );");

        await conn.ExecuteAsync($@"
            CREATE TABLE IF NOT EXISTS {T("PurchaseOrders")} (
                Id {pk},
                SupplierId INTEGER,
                SupplierName TEXT NOT NULL,
                OrderNumber TEXT NOT NULL,
                Status INTEGER NOT NULL DEFAULT 0,
                ExpectedDate TEXT,
                Notes TEXT,
                CreatedAt TEXT NOT NULL,
                CreatedByUserId INTEGER NOT NULL
            );");

        await conn.ExecuteAsync($@"
            CREATE TABLE IF NOT EXISTS {T("PurchaseOrderItems")} (
                Id {pk},
                PurchaseOrderId INTEGER NOT NULL,
                MedicineId INTEGER NOT NULL,
                MedicineName TEXT NOT NULL,
                Quantity INTEGER NOT NULL,
                Unit TEXT NOT NULL DEFAULT 'Box',
                UnitCost REAL NOT NULL,
                ReceivedQuantity INTEGER NOT NULL DEFAULT 0
            );");

        // Migration: add Unit column to PurchaseOrderItems if missing
        try
        {
            await conn.ExecuteAsync($"ALTER TABLE {T("PurchaseOrderItems")} ADD COLUMN Unit TEXT NOT NULL DEFAULT 'Box'");
        }
        catch { /* column already exists */ }

        await conn.ExecuteAsync($@"
            CREATE TABLE IF NOT EXISTS {T("GoodsReceipts")} (
                Id {pk},
                PurchaseOrderId INTEGER NOT NULL,
                ReceivedAt TEXT NOT NULL,
                ReceivedByUserId INTEGER NOT NULL,
                Notes TEXT
            );");

        await conn.ExecuteAsync($@"
            CREATE TABLE IF NOT EXISTS {T("GoodsReceiptItems")} (
                Id {pk},
                GoodsReceiptId INTEGER NOT NULL,
                PurchaseOrderItemId INTEGER NOT NULL,
                MedicineId INTEGER NOT NULL,
                MedicineName TEXT NOT NULL,
                OrderedQuantity INTEGER NOT NULL,
                ReceivedQuantity INTEGER NOT NULL,
                BatchNumber TEXT,
                ExpiryDate TEXT,
                UnitCost REAL NOT NULL
            );");

        await conn.ExecuteAsync($@"
            CREATE TABLE IF NOT EXISTS {T("Settings")} (
                Key TEXT PRIMARY KEY,
                Value TEXT NOT NULL
            );");

        await conn.ExecuteAsync($@"
            CREATE TABLE IF NOT EXISTS {T("Categories")} (
                Id {pk},
                Code TEXT,
                Name TEXT NOT NULL,
                Description TEXT,
                IsActive INTEGER NOT NULL DEFAULT 1,
                CreatedAt TEXT NOT NULL
            );");

        await conn.ExecuteAsync($@"
            CREATE TABLE IF NOT EXISTS {T("Customers")} (
                Id {pk},
                Name TEXT NOT NULL,
                Phone TEXT,
                Email TEXT,
                Address TEXT,
                IsActive INTEGER NOT NULL DEFAULT 1,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT,
                ApprovalStatus INTEGER NOT NULL DEFAULT 0,
                SubmittedByUserId INTEGER NOT NULL DEFAULT 0,
                SubmittedByName TEXT NOT NULL DEFAULT ''
            );");

        await conn.ExecuteAsync($@"
            CREATE TABLE IF NOT EXISTS {T("Suppliers")} (
                Id {pk},
                Name TEXT NOT NULL,
                ContactPerson TEXT,
                Phone TEXT,
                Email TEXT,
                Address TEXT,
                IsActive INTEGER NOT NULL DEFAULT 1,
                CreatedAt TEXT NOT NULL,
                ApprovalStatus INTEGER NOT NULL DEFAULT 0,
                SubmittedByUserId INTEGER NOT NULL DEFAULT 0,
                SubmittedByName TEXT NOT NULL DEFAULT ''
            );");

        await conn.ExecuteAsync($@"
            CREATE TABLE IF NOT EXISTS {T("StockAdjustments")} (
                Id {pk},
                MedicineId INTEGER NOT NULL,
                MedicineName TEXT NOT NULL,
                QuantityChange INTEGER NOT NULL,
                Reason TEXT NOT NULL,
                AdjustedByUserId INTEGER NOT NULL,
                AdjustedByName TEXT NOT NULL,
                CreatedAt TEXT NOT NULL
            );");

        await conn.ExecuteAsync($@"
            CREATE TABLE IF NOT EXISTS {T("CodeCounters")} (
                Prefix TEXT NOT NULL,
                Year INTEGER NOT NULL,
                Counter INTEGER NOT NULL DEFAULT 0,
                PRIMARY KEY (Prefix, Year)
            );");

        await conn.ExecuteAsync($@"
            CREATE TABLE IF NOT EXISTS {T("SaleReturns")} (
                Id {pk},
                MedicineId INTEGER NOT NULL,
                MedicineName TEXT NOT NULL,
                Quantity INTEGER NOT NULL,
                UnitPrice REAL NOT NULL,
                RefundAmount REAL NOT NULL,
                PaymentMethod TEXT NOT NULL,
                Reason TEXT NOT NULL,
                OriginalSaleId INTEGER NULL,
                ProcessedByUserId INTEGER NOT NULL,
                ProcessedByName TEXT NOT NULL,
                CreatedAt TEXT NOT NULL
            );");

        await conn.ExecuteAsync($@"
            CREATE TABLE IF NOT EXISTS {T("SalePayments")} (
                Id {pk},
                SaleId INTEGER NOT NULL,
                Amount REAL NOT NULL,
                PaidAt TEXT NOT NULL DEFAULT ({now}),
                Note TEXT NOT NULL DEFAULT ''
            );");

        await conn.ExecuteAsync($@"
            CREATE TABLE IF NOT EXISTS {T("PurchasePayments")} (
                Id {pk},
                PurchaseId INTEGER NOT NULL,
                Amount REAL NOT NULL,
                PaidAt TEXT NOT NULL DEFAULT ({now}),
                Note TEXT NOT NULL DEFAULT ''
            );");

        await conn.ExecuteAsync($@"
            CREATE TABLE IF NOT EXISTS {T("DailyClosings")} (
                Id {pk},
                ClosingDate TEXT NOT NULL,
                CashSales REAL NOT NULL DEFAULT 0,
                CardSales REAL NOT NULL DEFAULT 0,
                MobileSales REAL NOT NULL DEFAULT 0,
                InsuranceSales REAL NOT NULL DEFAULT 0,
                ExpectedCash REAL NOT NULL DEFAULT 0,
                ActualCash REAL NOT NULL DEFAULT 0,
                Difference REAL NOT NULL DEFAULT 0,
                Notes TEXT,
                ClosedByUserId INTEGER NOT NULL,
                ClosedByUserName TEXT NOT NULL,
                CreatedAt TEXT NOT NULL
            );");

        await conn.ExecuteAsync($@"
            CREATE TABLE IF NOT EXISTS {T("PendingSalePayments")} (
                Id {pk},
                SaleId INTEGER NOT NULL,
                CustomerName TEXT NOT NULL DEFAULT '',
                Amount REAL NOT NULL,
                Note TEXT NOT NULL DEFAULT '',
                SubmittedByUserId INTEGER NOT NULL,
                SubmittedByName TEXT NOT NULL DEFAULT '',
                SubmittedAt TEXT NOT NULL DEFAULT ({now}),
                ApprovalStatus INTEGER NOT NULL DEFAULT 0,
                RejectionReason TEXT
            );");

        await conn.ExecuteAsync($@"
            CREATE TABLE IF NOT EXISTS {T("PendingExpenses")} (
                Id {pk},
                Date TEXT NOT NULL,
                Category TEXT NOT NULL,
                Description TEXT,
                Amount REAL NOT NULL DEFAULT 0,
                SubmittedByUserId INTEGER NOT NULL,
                SubmittedByName TEXT NOT NULL DEFAULT '',
                SubmittedAt TEXT NOT NULL DEFAULT ({now}),
                ApprovalStatus INTEGER NOT NULL DEFAULT 0,
                RejectionReason TEXT
            );");

        await conn.ExecuteAsync($@"
            CREATE TABLE IF NOT EXISTS {T("Expenses")} (
                Id {pk},
                Code TEXT,
                Date TEXT NOT NULL,
                Category TEXT NOT NULL,
                Description TEXT,
                Amount REAL NOT NULL DEFAULT 0,
                CreatedBy TEXT,
                CreatedAt TEXT NOT NULL
            );");

        // Other Income — non-sale income sources (service fees, consultations,
        // delivery charges, etc). Kept intentionally separate from Sales revenue:
        // P&L, Dashboard and Accounting Overview do NOT include this table.
        await conn.ExecuteAsync($@"
            CREATE TABLE IF NOT EXISTS {T("UserSessions")} (
                Id {pk},
                UserId INTEGER NOT NULL,
                UserName TEXT NOT NULL DEFAULT '',
                LoginTime TEXT NOT NULL,
                LogoutTime TEXT,
                CreatedAt TEXT NOT NULL
            );");

        await conn.ExecuteAsync($@"
            CREATE TABLE IF NOT EXISTS {T("OtherIncomes")} (
                Id {pk},
                Code TEXT,
                Date TEXT NOT NULL,
                Category TEXT NOT NULL,
                Description TEXT,
                Amount REAL NOT NULL DEFAULT 0,
                CreatedBy TEXT,
                CreatedAt TEXT NOT NULL
            );");

        // ── Auto-migrate: add missing columns to existing SQLite databases ──
        // Skipped on Postgres — a fresh Postgres DB already gets the full
        // current schema from the CREATE TABLE statements above.
        if (!_context.IsPostgres)
        {
            var customerCols = (await conn.QueryAsync<string>("SELECT name FROM pragma_table_info('Customers')")).ToHashSet();
            if (!customerCols.Contains("ApprovalStatus"))
                await conn.ExecuteAsync("ALTER TABLE Customers ADD COLUMN ApprovalStatus INTEGER NOT NULL DEFAULT 1");

            var supplierCols = (await conn.QueryAsync<string>("SELECT name FROM pragma_table_info('Suppliers')")).ToHashSet();
            if (!supplierCols.Contains("ApprovalStatus"))
                await conn.ExecuteAsync("ALTER TABLE Suppliers ADD COLUMN ApprovalStatus INTEGER NOT NULL DEFAULT 1");

            if (!customerCols.Contains("SubmittedByUserId"))
                await conn.ExecuteAsync("ALTER TABLE Customers ADD COLUMN SubmittedByUserId INTEGER NOT NULL DEFAULT 0");
            if (!customerCols.Contains("SubmittedByName"))
                await conn.ExecuteAsync("ALTER TABLE Customers ADD COLUMN SubmittedByName TEXT NOT NULL DEFAULT ''");

            if (!supplierCols.Contains("SubmittedByUserId"))
                await conn.ExecuteAsync("ALTER TABLE Suppliers ADD COLUMN SubmittedByUserId INTEGER NOT NULL DEFAULT 0");
            if (!supplierCols.Contains("SubmittedByName"))
                await conn.ExecuteAsync("ALTER TABLE Suppliers ADD COLUMN SubmittedByName TEXT NOT NULL DEFAULT ''");

            var purchaseCols = (await conn.QueryAsync<string>("SELECT name FROM pragma_table_info('Purchases')")).ToHashSet();
            if (!purchaseCols.Contains("ApprovalStatus"))
                await conn.ExecuteAsync("ALTER TABLE Purchases ADD COLUMN ApprovalStatus INTEGER NOT NULL DEFAULT 1");

            if (!purchaseCols.Contains("SupplierId"))
                await conn.ExecuteAsync("ALTER TABLE Purchases ADD COLUMN SupplierId INTEGER");

            if (!purchaseCols.Contains("AmountPaid"))
                await conn.ExecuteAsync("ALTER TABLE Purchases ADD COLUMN AmountPaid REAL NOT NULL DEFAULT 0");

            if (!purchaseCols.Contains("PurchaseOrderId"))
                await conn.ExecuteAsync("ALTER TABLE Purchases ADD COLUMN PurchaseOrderId INTEGER");

            if (!purchaseCols.Contains("GoodsReceiptId"))
                await conn.ExecuteAsync("ALTER TABLE Purchases ADD COLUMN GoodsReceiptId INTEGER");

            var goodsReceiptCols = (await conn.QueryAsync<string>("SELECT name FROM pragma_table_info('GoodsReceipts')")).ToHashSet();
            if (!goodsReceiptCols.Contains("ApprovalStatus"))
                await conn.ExecuteAsync("ALTER TABLE GoodsReceipts ADD COLUMN ApprovalStatus INTEGER NOT NULL DEFAULT 1");
            if (!goodsReceiptCols.Contains("RejectionReason"))
                await conn.ExecuteAsync("ALTER TABLE GoodsReceipts ADD COLUMN RejectionReason TEXT");

            var categoryCols = (await conn.QueryAsync<string>("SELECT name FROM pragma_table_info('Categories')")).ToHashSet();
            if (!categoryCols.Contains("Code"))
                await conn.ExecuteAsync("ALTER TABLE Categories ADD COLUMN Code TEXT");

            var medicineCols = (await conn.QueryAsync<string>("SELECT name FROM pragma_table_info('Medicines')")).ToHashSet();
            if (!medicineCols.Contains("Barcode"))
                await conn.ExecuteAsync("ALTER TABLE Medicines ADD COLUMN Barcode TEXT");

            if (!medicineCols.Contains("Supplier"))
                await conn.ExecuteAsync("ALTER TABLE Medicines ADD COLUMN Supplier TEXT");

            if (!medicineCols.Contains("CostPrice"))
                await conn.ExecuteAsync("ALTER TABLE Medicines ADD COLUMN CostPrice REAL NOT NULL DEFAULT 0");

            var userCols = (await conn.QueryAsync<string>("SELECT name FROM pragma_table_info('Users')")).ToHashSet();
            if (!userCols.Contains("SecurityQuestion"))
                await conn.ExecuteAsync("ALTER TABLE Users ADD COLUMN SecurityQuestion TEXT");

            if (!userCols.Contains("SecurityAnswerHash"))
                await conn.ExecuteAsync("ALTER TABLE Users ADD COLUMN SecurityAnswerHash TEXT");

            if (!userCols.Contains("AvatarPath"))
                await conn.ExecuteAsync("ALTER TABLE Users ADD COLUMN AvatarPath TEXT");

            if (!userCols.Contains("Permissions"))
                await conn.ExecuteAsync("ALTER TABLE Users ADD COLUMN Permissions INTEGER NOT NULL DEFAULT 0");

            var saleCols = (await conn.QueryAsync<string>("SELECT name FROM pragma_table_info('Sales')")).ToHashSet();
            if (!saleCols.Contains("CustomerId"))
                await conn.ExecuteAsync("ALTER TABLE Sales ADD COLUMN CustomerId INTEGER");

            if (!saleCols.Contains("AmountPaid"))
                await conn.ExecuteAsync("ALTER TABLE Sales ADD COLUMN AmountPaid REAL NOT NULL DEFAULT 0");

            if (!saleCols.Contains("CustomerName"))
                await conn.ExecuteAsync("ALTER TABLE Sales ADD COLUMN CustomerName TEXT NOT NULL DEFAULT 'Walk-in Customer'");

            if (!saleCols.Contains("PaymentMethod"))
                await conn.ExecuteAsync("ALTER TABLE Sales ADD COLUMN PaymentMethod TEXT NOT NULL DEFAULT 'Cash'");

            if (!saleCols.Contains("TotalDiscount"))
                await conn.ExecuteAsync("ALTER TABLE Sales ADD COLUMN TotalDiscount REAL NOT NULL DEFAULT 0");

            if (!saleCols.Contains("ChangeDue"))
                await conn.ExecuteAsync("ALTER TABLE Sales ADD COLUMN ChangeDue REAL NOT NULL DEFAULT 0");

            // Migration: add PaymentMethod to Expenses
            var expenseCols = (await conn.QueryAsync<string>("SELECT name FROM pragma_table_info('Expenses')")).ToHashSet();
            if (!expenseCols.Contains("PaymentMethod"))
                await conn.ExecuteAsync("ALTER TABLE Expenses ADD COLUMN PaymentMethod TEXT NOT NULL DEFAULT 'Cash'");
            if (!expenseCols.Contains("Code"))
                await conn.ExecuteAsync("ALTER TABLE Expenses ADD COLUMN Code TEXT");

            // Migration: add PaymentMethod to OtherIncomes
            var otherIncomeCols = (await conn.QueryAsync<string>("SELECT name FROM pragma_table_info('OtherIncomes')")).ToHashSet();
            if (!otherIncomeCols.Contains("PaymentMethod"))
                await conn.ExecuteAsync("ALTER TABLE OtherIncomes ADD COLUMN PaymentMethod TEXT NOT NULL DEFAULT 'Cash'");
            if (!otherIncomeCols.Contains("Code"))
                await conn.ExecuteAsync("ALTER TABLE OtherIncomes ADD COLUMN Code TEXT");
        }
        else
        {
            // Postgres: ADD COLUMN IF NOT EXISTS is safe to run unconditionally every startup.
            await conn.ExecuteAsync($"ALTER TABLE {T("Expenses")} ADD COLUMN IF NOT EXISTS PaymentMethod TEXT NOT NULL DEFAULT 'Cash';");
            await conn.ExecuteAsync($"ALTER TABLE {T("Expenses")} ADD COLUMN IF NOT EXISTS Code TEXT;");
            await conn.ExecuteAsync($"ALTER TABLE {T("OtherIncomes")} ADD COLUMN IF NOT EXISTS PaymentMethod TEXT NOT NULL DEFAULT 'Cash';");
            await conn.ExecuteAsync($"ALTER TABLE {T("OtherIncomes")} ADD COLUMN IF NOT EXISTS Code TEXT;");
        }

        // ── End auto-migrate ──

        var existingAdmin = await conn.QueryFirstOrDefaultAsync<int?>(
            $"SELECT Id FROM {T("Users")} WHERE Username = 'admin'");

        if (existingAdmin == null)
        {
            var hash = BCrypt.Net.BCrypt.HashPassword("Admin@123");
            await conn.ExecuteAsync($@"
                INSERT INTO {T("Users")} (Username, PasswordHash, FullName, Role, IsActive, Permissions, CreatedAt)
                VALUES ('admin', @Hash, 'System Administrator', 0, 1, @Perms, {now});",
                new { Hash = hash, Perms = (long)Permission.All });
        }
    }
}
