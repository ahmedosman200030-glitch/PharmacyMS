namespace PharmacyMS.Infrastructure.Data;

public enum DbNetworkMode
{
    Offline,
    LocalNetwork,
    Cloud
}

public class DbConfig
{
    public DbProvider Provider { get; set; } = DbProvider.Sqlite;
    public DbNetworkMode NetworkMode { get; set; } = DbNetworkMode.Offline;
    public string? PostgresConnectionString { get; set; }
}
