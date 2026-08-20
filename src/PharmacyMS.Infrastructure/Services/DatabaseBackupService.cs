using PharmacyMS.Application.Interfaces.Services;
using PharmacyMS.Infrastructure.Data;

namespace PharmacyMS.Infrastructure.Services;

public class DatabaseBackupService : IDatabaseBackupService
{
    private readonly AppDbContext _context;
    public DatabaseBackupService(AppDbContext context) => _context = context;

    public DatabaseInfo GetDatabaseInfo()
    {
        if (_context.IsPostgres)
        {
            return new DatabaseInfo
            {
                Path = _context.GetSafeDisplayString(),
                SizeBytes = 0,
                LastModified = DateTime.MinValue
            };
        }

        var path = _context.GetDatabasePath();
        var fi = new FileInfo(path);
        return new DatabaseInfo { Path = path, SizeBytes = fi.Exists ? fi.Length : 0, LastModified = fi.Exists ? fi.LastWriteTime : DateTime.MinValue };
    }

    public Task<string> BackupAsync(string? destinationPath = null)
    {
        if (_context.IsPostgres)
        {
            throw new NotSupportedException(
                "Local file backup isn't available for cloud (Postgres/Supabase) databases. " +
                "Use Supabase's built-in backups instead: Dashboard → Database → Backups.");
        }

        var dbPath = _context.GetDatabasePath();
        string destPath;
        if (!string.IsNullOrWhiteSpace(destinationPath))
        {
            destPath = destinationPath;
            var destFolder = Path.GetDirectoryName(destPath);
            if (!string.IsNullOrEmpty(destFolder)) Directory.CreateDirectory(destFolder);
        }
        else
        {
            var folder = Path.Combine(Path.GetDirectoryName(dbPath)!, "Backups");
            Directory.CreateDirectory(folder);
            destPath = Path.Combine(folder, $"pharmacyms-backup-{DateTime.Now:yyyyMMdd-HHmmss}.db");
        }
        File.Copy(dbPath, destPath, overwrite: true);
        return Task.FromResult(destPath);
    }

    public Task RestoreAsync(string backupFilePath)
    {
        if (_context.IsPostgres)
        {
            throw new NotSupportedException(
                "Restoring from a local file isn't available for cloud (Postgres/Supabase) databases. " +
                "Use Supabase's built-in restore tools instead.");
        }

        var dbPath = _context.GetDatabasePath();
        File.Copy(backupFilePath, dbPath, overwrite: true);
        return Task.CompletedTask;
    }
}
