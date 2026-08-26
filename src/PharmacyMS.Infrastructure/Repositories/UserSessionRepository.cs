using Dapper;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Domain.Entities;
using PharmacyMS.Infrastructure.Data;

namespace PharmacyMS.Infrastructure.Repositories;

public class UserSessionRepository : IUserSessionRepository
{
    private readonly AppDbContext _context;
    public UserSessionRepository(AppDbContext context) => _context = context;

    public async Task<int> CreateAsync(UserSession session)
    {
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<int>($@"
            INSERT INTO UserSessions (UserId, UserName, LoginTime, LogoutTime, CreatedAt)
            VALUES (@UserId, @UserName, @LoginTime, @LogoutTime, @CreatedAt)
            {_context.InsertIdSuffix()};", session);
    }

    public async Task CloseSessionAsync(int sessionId, DateTime logoutTime)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE UserSessions SET LogoutTime=@LogoutTime WHERE Id=@Id",
            new { LogoutTime = logoutTime, Id = sessionId });
    }

    public async Task<IEnumerable<UserSession>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<UserSession>(
            "SELECT * FROM UserSessions WHERE LoginTime >= @From AND LoginTime < @ToExclusive ORDER BY LoginTime DESC",
            new { From = from.Date.ToString("yyyy-MM-dd"), ToExclusive = to.Date.AddDays(1).ToString("yyyy-MM-dd") });
    }
}
