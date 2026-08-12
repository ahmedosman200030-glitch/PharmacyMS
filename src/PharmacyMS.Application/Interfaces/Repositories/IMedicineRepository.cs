using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Application.Interfaces.Repositories;

public interface IMedicineRepository
{
    Task<Medicine?> GetByIdAsync(int id);
    Task<IEnumerable<Medicine>> GetAllAsync();
    Task<IEnumerable<Medicine>> SearchAsync(string term);
    Task<IEnumerable<Medicine>> GetLowStockAsync();
    Task<int> CreateAsync(Medicine medicine);
    Task UpdateAsync(Medicine medicine);
    Task DeleteAsync(int id);
}
