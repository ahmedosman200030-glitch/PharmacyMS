using System.Collections.ObjectModel;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Desktop.ViewModels;

public class UsersViewModel
{
    private readonly IUserRepository _repository;

    public ObservableCollection<User> Users { get; } = new();

    public UsersViewModel(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task LoadAsync()
    {
        Users.Clear();
        var items = await _repository.GetAllAsync();
        foreach (var u in items)
            Users.Add(u);
    }

    public async Task<int> AddAsync(User user, string plainPassword)
    {
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword);
        var id = await _repository.CreateAsync(user);
        user.Id = id;
        Users.Add(user);
        return id;
    }

    public async Task UpdateAsync(User user)
    {
        await _repository.UpdateAsync(user);
    }

    public async Task DeactivateAsync(User user)
    {
        await _repository.DeleteAsync(user.Id);
        user.IsActive = false;
    }

    public async Task ActivateAsync(User user)
    {
        await _repository.ActivateAsync(user.Id);
        user.IsActive = true;
    }
}
