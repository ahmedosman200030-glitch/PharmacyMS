using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Application.Interfaces.Repositories;

public interface IUserSessionRepository
{
    Task<int> CreateAsync(UserSession session);
    Task CloseSessionAsync(int sessionId, DateTime logoutTime);
    Task<IEnumerable<UserSession>> GetByDateRangeAsync(DateTime from, DateTime to);
}
