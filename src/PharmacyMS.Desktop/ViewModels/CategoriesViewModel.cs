using System.Collections.ObjectModel;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Desktop.ViewModels;

public class CategoriesViewModel
{
    private readonly ICategoryRepository _repository;

    public ObservableCollection<Category> Categories { get; } = new();

    public CategoriesViewModel(ICategoryRepository repository)
    {
        _repository = repository;
    }

    public async Task LoadAsync()
    {
        Categories.Clear();
        var items = await _repository.GetAllAsync();
        foreach (var c in items)
            Categories.Add(c);
    }

    public async Task<int> AddAsync(Category category)
    {
        var id = await _repository.CreateAsync(category);
        category.Id = id;
        Categories.Add(category);
        return id;
    }

    public async Task UpdateAsync(Category category) => await _repository.UpdateAsync(category);

    public async Task DeleteAsync(Category category)
    {
        await _repository.DeleteAsync(category.Id);
        Categories.Remove(category);
    }
}
