using PharmacyMS.Domain.Entities;
using PharmacyMS.Domain.Enums;

namespace PharmacyMS.Application.Interfaces.Repositories;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(int id);
    Task<IEnumerable<Customer>> GetAllAsync();
    Task<IEnumerable<Customer>> SearchAsync(string term);
    Task<int> CreateAsync(Customer customer);
    Task UpdateAsync(Customer customer);
    Task DeleteAsync(int id);
    Task<Customer> GetOrCreateByNameAsync(string name);
    Task<decimal> GetOutstandingBalanceAsync(int customerId);
    Task UpdateApprovalStatusAsync(int id, ApprovalStatus status);
    Task<IEnumerable<Customer>> GetBySubmitterAsync(int userId);
}
