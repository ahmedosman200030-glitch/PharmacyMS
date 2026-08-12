using Dapper;
using PharmacyMS.Application.Interfaces.Services;
using PharmacyMS.Infrastructure.Data;

namespace PharmacyMS.Infrastructure.Services;

public class DatabaseBackupService : IDatabaseBackupService
{
    private readonly AppDbContext _context;

    public DatabaseBackupService(AppDbContext context)
    {
        _context = context;
    }

    public DatabaseInfo GetDatabaseInfo()
    {
        // With PostgreSQL, there's no local file — return connection info instead
        return new DatabaseInfo
        {
            Path = "PostgreSQL (Supabase Cloud)",
            SizeBytes = 0,
            LastModified = DateTime.UtcNow
        };
    }

    public async Task<string> BackupAsync(string? destinationPath = null)
    {
        // Export all tables to a SQL-like CSV dump in the user's Documents folder
        var folder = destinationPath
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "PharmacyMS", "Backups");

        Directory.CreateDirectory(folder);
        var backupName = $"pharmacyms-backup-{DateTime.Now:yyyyMMdd-HHmmss}";
        var backupDir = Path.Combine(folder, backupName);
        Directory.CreateDirectory(backupDir);

        using var conn = _context.CreateConnection();
        await conn.OpenAsync();

        var tables = new[]
        {
            "Users", "Medicines", "Sales", "SaleItems", "Purchases",
            "PurchaseItems", "Customers", "Suppliers", "Categories",
            "StockAdjustments", "DailyClosings", "Settings"
        };

        foreach (var table in tables)
        {
            var rows = await conn.QueryAsync($"SELECT * FROM \"{table}\"");
            var lines = new List<string>();
            foreach (var row in rows)
            {
                var dict = (IDictionary<string, object?>)row;
                if (lines.Count == 0)
                    lines.Add(string.Join(",", dict.Keys));
                lines.Add(string.Join(",", dict.Values.Select(v =>
                    v == null ? "" : $"\"{v.ToString()!.Replace("\"", "\"\"")}\"")));
            }
            await File.WriteAllLinesAsync(Path.Combine(backupDir, $"{table}.csv"), lines);
        }

        return backupDir;
    }

    public Task RestoreAsync(string backupFilePath)
    {
        // Restore from CSV backup is complex — for now inform the user
        throw new NotSupportedException(
            "Restore from backup requires manual import. " +
            "Please contact your system administrator to restore from a cloud backup.");
    }
}
