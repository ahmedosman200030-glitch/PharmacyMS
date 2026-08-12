using Avalonia.Controls;
using PharmacyMS.Desktop.ViewModels;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Desktop.Views.Suppliers;

public partial class SuppliersView : UserControl
{
    private readonly SuppliersViewModel _vm;
    public SuppliersView() { InitializeComponent(); }
    public SuppliersView(SuppliersViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        Grid.ItemsSource = _vm.Suppliers;
        AttachedToVisualTree += async (_, _) => await _vm.LoadAsync();

        AddButton.Click += async (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(NameBox.Text))
            {
                var s = new Supplier
                {
                    Name = NameBox.Text.Trim(),
                    ContactPerson = ContactBox.Text?.Trim(),
                    Phone = PhoneBox.Text?.Trim(),
                    Email = EmailBox.Text?.Trim()
                };
                await _vm.AddAsync(s);
                NameBox.Text = ""; ContactBox.Text = ""; PhoneBox.Text = ""; EmailBox.Text = "";
            }
        };

        DeleteButton.Click += async (_, _) =>
        {
            if (Grid.SelectedItem is Supplier s)
                await _vm.DeleteAsync(s);
        };
    }
}
