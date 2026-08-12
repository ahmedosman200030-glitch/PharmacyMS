using Npgsql;

namespace PharmacyMS.Infrastructure.Data;

public class AppDbContext
{
    private readonly string _connectionString;

    public AppDbContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    public NpgsqlConnection CreateConnection() => new NpgsqlConnection(_connectionString);

    public static string DefaultConnectionString()
    {
        // Reads from environment variable so each branch can have its own config
        return Environment.GetEnvironmentVariable("PHARMACYMS_CONNECTION_STRING")
            ?? throw new InvalidOperationException(
                "Connection string not set. Please set the PHARMACYMS_CONNECTION_STRING environment variable.");
    }
}
