using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Application.Interfaces.Repositories;

public interface ICategoryRepository
{
    Task<IEnumerable<Category>> GetAllAsync();
    Task<int> CreateAsync(Category category);
    Task UpdateAsync(Category category);
    Task DeleteAsync(int id);
}
