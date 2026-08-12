using Avalonia.Controls;
using PharmacyMS.Desktop.ViewModels;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Desktop.Views.Categories;

public partial class CategoriesView : UserControl
{
    private readonly CategoriesViewModel _vm;
    public CategoriesView() { InitializeComponent(); }
    public CategoriesView(CategoriesViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        Grid.ItemsSource = _vm.Categories;
        AttachedToVisualTree += async (_, _) => await _vm.LoadAsync();

        AddButton.Click += async (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(NameBox.Text))
            {
                var cat = new Category { Name = NameBox.Text.Trim(), Description = DescBox.Text?.Trim() ?? "" };
                await _vm.AddAsync(cat);
                NameBox.Text = ""; DescBox.Text = "";
            }
        };

        DeleteButton.Click += async (_, _) =>
        {
            if (Grid.SelectedItem is Category c)
                await _vm.DeleteAsync(c);
        };
    }
}
