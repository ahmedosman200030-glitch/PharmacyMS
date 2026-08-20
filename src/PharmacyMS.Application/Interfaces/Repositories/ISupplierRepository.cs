using PharmacyMS.Domain.Entities;
using PharmacyMS.Domain.Enums;

namespace PharmacyMS.Application.Interfaces.Repositories;

public interface ISupplierRepository
{
    Task<IEnumerable<Supplier>> GetAllAsync();
    Task<int> CreateAsync(Supplier supplier);
    Task UpdateAsync(Supplier supplier);
    Task DeleteAsync(int id);
    Task UpdateApprovalStatusAsync(int id, ApprovalStatus status);
    Task<IEnumerable<Supplier>> GetBySubmitterAsync(int userId);
}
