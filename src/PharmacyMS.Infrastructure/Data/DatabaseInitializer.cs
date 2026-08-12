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

    public async Task InitializeAsync()
    {
        using var conn = _context.CreateConnection();
        await conn.OpenAsync();

        await conn.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS Users (
                Id SERIAL PRIMARY KEY,
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
                Permissions BIGINT NOT NULL DEFAULT 0
            );");

        await conn.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS Medicines (
                Id SERIAL PRIMARY KEY,
                Name TEXT NOT NULL,
                GenericName TEXT,
                Category TEXT,
                Manufacturer TEXT,
                UnitPrice DOUBLE PRECISION NOT NULL DEFAULT 0,
                QuantityInStock INTEGER NOT NULL DEFAULT 0,
                ReorderLevel INTEGER NOT NULL DEFAULT 10,
                ExpiryDate TEXT,
                BatchNumber TEXT,
                IsActive INTEGER NOT NULL DEFAULT 1,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT,
                Barcode TEXT,
                Supplier TEXT,
                CostPrice DOUBLE PRECISION NOT NULL DEFAULT 0
            );");

        await conn.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS Sales (
                Id SERIAL PRIMARY KEY,
                InvoiceNumber TEXT NOT NULL,
                CashierId INTEGER NOT NULL,
                TotalAmount DOUBLE PRECISION NOT NULL,
                CreatedAt TEXT NOT NULL,
                Subtotal DOUBLE PRECISION NOT NULL DEFAULT 0,
                TaxRate DOUBLE PRECISION NOT NULL DEFAULT 0,
                TaxAmount DOUBLE PRECISION NOT NULL DEFAULT 0,
                CustomerId INTEGER,
                AmountPaid DOUBLE PRECISION NOT NULL DEFAULT 0,
                CustomerName TEXT NOT NULL DEFAULT 'Walk-in Customer',
                PaymentMethod TEXT NOT NULL DEFAULT 'Cash',
                TotalDiscount DOUBLE PRECISION NOT NULL DEFAULT 0,
                ChangeDue DOUBLE PRECISION NOT NULL DEFAULT 0
            );");

        await conn.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS DailyClosings (
                Id SERIAL PRIMARY KEY,
                ClosingDate TEXT NOT NULL,
                CashSales DOUBLE PRECISION NOT NULL DEFAULT 0,
                CardSales DOUBLE PRECISION NOT NULL DEFAULT 0,
                MobileSales DOUBLE PRECISION NOT NULL DEFAULT 0,
                InsuranceSales DOUBLE PRECISION NOT NULL DEFAULT 0,
                ExpectedCash DOUBLE PRECISION NOT NULL DEFAULT 0,
                ActualCash DOUBLE PRECISION NOT NULL DEFAULT 0,
                Difference DOUBLE PRECISION NOT NULL DEFAULT 0,
                Notes TEXT,
                ClosedByUserId INTEGER NOT NULL,
                ClosedByUserName TEXT NOT NULL,
                CreatedAt TEXT NOT NULL
            );");

        await conn.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS SaleItems (
                Id SERIAL PRIMARY KEY,
                SaleId INTEGER NOT NULL,
                MedicineId INTEGER NOT NULL,
                MedicineName TEXT NOT NULL,
                UnitPrice DOUBLE PRECISION NOT NULL,
                Quantity INTEGER NOT NULL
            );");

        await conn.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS Purchases (
                Id SERIAL PRIMARY KEY,
                SupplierName TEXT NOT NULL,
                InvoiceNumber TEXT,
                TotalAmount DOUBLE PRECISION NOT NULL,
                CreatedAt TEXT NOT NULL,
                SupplierId INTEGER,
                AmountPaid DOUBLE PRECISION NOT NULL DEFAULT 0
            );");

        await conn.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS PurchaseItems (
                Id SERIAL PRIMARY KEY,
                PurchaseId INTEGER NOT NULL,
                MedicineId INTEGER NOT NULL,
                MedicineName TEXT NOT NULL,
                UnitCost DOUBLE PRECISION NOT NULL,
                Quantity INTEGER NOT NULL,
                BatchNumber TEXT,
                ExpiryDate TEXT
            );");

        await conn.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS Settings (
                Key TEXT PRIMARY KEY,
                Value TEXT NOT NULL
            );");

        await conn.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS Categories (
                Id SERIAL PRIMARY KEY,
                Name TEXT NOT NULL,
                ContactPerson TEXT,
                Phone TEXT,
                Email TEXT,
                Address TEXT,
                IsActive INTEGER NOT NULL DEFAULT 1,
                CreatedAt TEXT NOT NULL
            );");

        await conn.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS Customers (
                Id SERIAL PRIMARY KEY,
                Name TEXT NOT NULL,
                Phone TEXT,
                Email TEXT,
                Address TEXT,
                IsActive INTEGER NOT NULL DEFAULT 1,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT
            );");

        await conn.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS Suppliers (
                Id SERIAL PRIMARY KEY,
                Name TEXT NOT NULL,
                ContactPerson TEXT,
                Phone TEXT,
                Email TEXT,
                Address TEXT,
                IsActive INTEGER NOT NULL DEFAULT 1,
                CreatedAt TEXT NOT NULL
            );");

        await conn.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS StockAdjustments (
                Id SERIAL PRIMARY KEY,
                MedicineId INTEGER NOT NULL,
                MedicineName TEXT NOT NULL,
                QuantityChange INTEGER NOT NULL,
                Reason TEXT NOT NULL,
                AdjustedByUserId INTEGER NOT NULL,
                AdjustedByName TEXT NOT NULL,
                CreatedAt TEXT NOT NULL
            );");

        // Seed admin user if not exists
        var existingAdmin = await conn.QueryFirstOrDefaultAsync<int?>(
            "SELECT Id FROM Users WHERE Username = 'admin'");

        if (existingAdmin == null)
        {
            var hash = BCrypt.Net.BCrypt.HashPassword("Admin@123");
            await conn.ExecuteAsync(@"
                INSERT INTO Users (Username, PasswordHash, FullName, Role, IsActive, Permissions, CreatedAt)
                VALUES ('admin', @Hash, 'System Administrator', 0, 1, @Perms, now()::text);",
                new { Hash = hash, Perms = (long)Permission.All });
        }
    }
}
