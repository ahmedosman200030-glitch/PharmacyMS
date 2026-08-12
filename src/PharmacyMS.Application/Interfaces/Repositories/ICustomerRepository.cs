using PharmacyMS.Domain.Entities;

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
}
