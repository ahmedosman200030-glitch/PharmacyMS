namespace PharmacyMS.Infrastructure.Data;

public class DbConfig
{
    public DbProvider Provider { get; set; } = DbProvider.Sqlite;
    public string? PostgresConnectionString { get; set; }
}
