using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Application.Interfaces.Repositories;

public interface IOtherIncomeRepository
{
    Task<IEnumerable<OtherIncome>> GetAllAsync();
    Task<IEnumerable<OtherIncome>> GetByDateRangeAsync(DateTime from, DateTime to);
    Task<int> CreateAsync(OtherIncome income);
    Task UpdateAsync(OtherIncome income);
    Task DeleteAsync(int id);
    Task<decimal> GetTotalByDateRangeAsync(DateTime from, DateTime to);
}
