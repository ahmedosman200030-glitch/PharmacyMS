using Dapper;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Domain.Entities;
using PharmacyMS.Infrastructure.Data;

namespace PharmacyMS.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM Users WHERE Username = @Username AND IsActive = 1",
            new { Username = username });
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM Users WHERE Id = @Id",
            new { Id = id });
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<User>(
            "SELECT * FROM Users ORDER BY FullName");
    }

    public async Task<int> CreateAsync(User user)
    {
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO Users (Username, PasswordHash, FullName, Role, IsActive, Permissions, CreatedAt)
            VALUES (@Username, @PasswordHash, @FullName, @Role, @IsActive, @Permissions, datetime('now'));
            SELECT last_insert_rowid();",
            user);
    }

    public async Task UpdateAsync(User user)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(@"
            UPDATE Users SET
                FullName           = @FullName,
                Role               = @Role,
                IsActive           = @IsActive,
                SecurityQuestion   = @SecurityQuestion,
                SecurityAnswerHash = @SecurityAnswerHash,
                AvatarPath         = @AvatarPath,
                Permissions        = @Permissions,
                UpdatedAt          = datetime('now')
            WHERE Id = @Id",
            user);
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE Users SET IsActive = 0 WHERE Id = @Id",
            new { Id = id });
    }

    public async Task ActivateAsync(int id)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE Users SET IsActive = 1 WHERE Id = @Id",
            new { Id = id });
    }

    public async Task UpdateLastLoginAsync(int userId)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE Users SET LastLogin = datetime('now') WHERE Id = @Id",
            new { Id = userId });
    }
}
