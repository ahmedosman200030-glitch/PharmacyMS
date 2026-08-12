namespace PharmacyMS.Application.Interfaces.Services;

public class DatabaseInfo
{
    public string Path { get; set; } = "";
    public long SizeBytes { get; set; }
    public DateTime LastModified { get; set; }
}

public interface IDatabaseBackupService
{
    DatabaseInfo GetDatabaseInfo();
    Task<string> BackupAsync(string? destinationPath = null);
    Task RestoreAsync(string backupFilePath);
}
