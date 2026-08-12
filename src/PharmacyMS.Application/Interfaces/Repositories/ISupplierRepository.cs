using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Application.Interfaces.Repositories;

public interface ISupplierRepository
{
    Task<IEnumerable<Supplier>> GetAllAsync();
    Task<int> CreateAsync(Supplier supplier);
    Task UpdateAsync(Supplier supplier);
    Task DeleteAsync(int id);
}
