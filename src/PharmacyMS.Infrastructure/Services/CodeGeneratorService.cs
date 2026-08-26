using Dapper;
using PharmacyMS.Infrastructure.Data;

namespace PharmacyMS.Infrastructure.Services;

/// <summary>
/// Generates unique, sequential codes like EXP-2026-000001 / INC-2026-000001.
/// Atomic across multiple PCs sharing the same cloud database: the counter increment
/// happens inside a single UPDATE/UPSERT statement so two concurrent callers can never
/// receive the same number, even against Postgres or SQLite.
/// </summary>
public class CodeGeneratorService
{
    private readonly AppDbContext _context;
    public CodeGeneratorService(AppDbContext context) => _context = context;

    public async Task<string> GetNextCodeAsync(string prefix)
    {
        var year = DateTime.Now.Year;
        using var conn = _context.CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();

        int nextValue;
        if (_context.IsPostgres)
        {
            nextValue = await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO CodeCounters (Prefix, Year, Counter)
                VALUES (@Prefix, @Year, 1)
                ON CONFLICT (Prefix, Year) DO UPDATE SET Counter = CodeCounters.Counter + 1
                RETURNING Counter;",
                new { Prefix = prefix, Year = year }, tx);
        }
        else
        {
            // SQLite: no ON CONFLICT...DO UPDATE...RETURNING with WHERE-safe atomicity across
            // multi-process access, so use INSERT OR IGNORE + UPDATE inside this transaction.
            await conn.ExecuteAsync(
                "INSERT OR IGNORE INTO CodeCounters (Prefix, Year, Counter) VALUES (@Prefix, @Year, 0);",
                new { Prefix = prefix, Year = year }, tx);

            await conn.ExecuteAsync(
                "UPDATE CodeCounters SET Counter = Counter + 1 WHERE Prefix = @Prefix AND Year = @Year;",
                new { Prefix = prefix, Year = year }, tx);

            nextValue = await conn.ExecuteScalarAsync<int>(
                "SELECT Counter FROM CodeCounters WHERE Prefix = @Prefix AND Year = @Year;",
                new { Prefix = prefix, Year = year }, tx);
        }

        tx.Commit();
        return $"{prefix}-{year}-{nextValue:D6}";
    }
}
