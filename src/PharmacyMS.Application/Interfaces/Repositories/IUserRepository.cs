using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByIdAsync(int id);
    Task<IEnumerable<User>> GetAllAsync();
    Task<int> CreateAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(int id);
    Task ActivateAsync(int id);
    Task UpdateLastLoginAsync(int userId);
}
