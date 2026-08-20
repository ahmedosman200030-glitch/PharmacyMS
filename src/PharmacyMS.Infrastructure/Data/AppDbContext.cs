using System.Data.Common;
using Microsoft.Data.Sqlite;
using Npgsql;

namespace PharmacyMS.Infrastructure.Data;

public class AppDbContext
{
    private readonly DbProvider _provider;
    private readonly string _connectionString;

    public AppDbContext(DbProvider provider, string connectionString)
    {
        _provider = provider;
        _connectionString = connectionString;
    }

    public DbProvider Provider => _provider;
    public bool IsPostgres => _provider == DbProvider.Postgres;

    public DbConnection CreateConnection() => _provider switch
    {
        DbProvider.Postgres => new NpgsqlConnection(_connectionString),
        _ => new SqliteConnection(_connectionString)
    };

    public string GetDatabasePath()
    {
        if (_provider == DbProvider.Postgres)
            return _connectionString;

        const string prefix = "Data Source=";
        var idx = _connectionString.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return _connectionString;
        var rest = _connectionString.Substring(idx + prefix.Length);
        var semi = rest.IndexOf(';');
        return semi >= 0 ? rest.Substring(0, semi) : rest;
    }

    /// <summary>
    /// Safe-for-display version of the connection info — host and database name only,
    /// never the password. Use this anywhere the connection info is shown in the UI.
    /// </summary>
    public string GetSafeDisplayString()
    {
        if (_provider != DbProvider.Postgres)
            return GetDatabasePath();

        try
        {
            var builder = new NpgsqlConnectionStringBuilder(_connectionString);
            return $"{builder.Host} / {builder.Database} (cloud)";
        }
        catch
        {
            return "Cloud database (Postgres)";
        }
    }

    public static string DefaultSqliteConnectionString()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PharmacyMS");
        Directory.CreateDirectory(folder);
        var dbPath = Path.Combine(folder, "pharmacyms.db");
        return $"Data Source={dbPath}";
    }

    public static string DefaultConnectionString() => DefaultSqliteConnectionString();

    public string NowExpr() => IsPostgres ? "NOW()" : "datetime('now')";

    public string AutoIncrementPk() => IsPostgres ? "SERIAL PRIMARY KEY" : "INTEGER PRIMARY KEY AUTOINCREMENT";

    public string InsertIdSuffix() => IsPostgres ? "RETURNING Id" : "; SELECT last_insert_rowid()";

    public string DateExpr(string column) => IsPostgres ? $"{column}::date" : $"CAST(DATE({column}) AS TEXT)";

    // Same date, but formatted so Microsoft.Data.Sqlite won't auto-convert the
    // returned string into a DateOnly (it only does this for yyyy-MM-dd text).
    public string DateDisplayExpr(string column) => IsPostgres
        ? $"to_char({column}::date, 'YYYY-MM-DD')"
        : $"strftime('%Y/%m/%d', {column})";

    public string YearMonthExpr(string column) => IsPostgres ? $"to_char({column}::timestamp, 'YYYY-MM')" : $"strftime('%Y-%m',{column})";
}
