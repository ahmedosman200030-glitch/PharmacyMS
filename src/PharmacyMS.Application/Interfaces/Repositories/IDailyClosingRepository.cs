using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Application.Interfaces.Repositories;

public interface IDailyClosingRepository
{
    Task<bool> HasClosedTodayAsync();
    Task<int> CreateAsync(DailyClosing closing);
    Task<List<DailyClosing>> GetHistoryAsync();
}
