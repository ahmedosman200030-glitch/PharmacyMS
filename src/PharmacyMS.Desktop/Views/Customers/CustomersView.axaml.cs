using Avalonia.Controls;
using PharmacyMS.Desktop.ViewModels;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Desktop.Views.Customers;

public partial class CustomersView : UserControl
{
    private readonly CustomersViewModel _vm;
    public CustomersView() { InitializeComponent(); }
    public CustomersView(CustomersViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        Grid.ItemsSource = _vm.Customers;
        AttachedToVisualTree += async (_, _) => await _vm.LoadAsync();

        AddButton.Click += async (_, _) =>
        {
            var form = new CustomerFormWindow();
            var result = await form.ShowDialog<Customer?>(TopLevel.GetTopLevel(this) as Avalonia.Controls.Window);
            if (result != null) await _vm.AddAsync(result);
        };

        EditButton.Click += async (_, _) =>
        {
            if (Grid.SelectedItem is not Customer c) return;
            var form = new CustomerFormWindow(c);
            var result = await form.ShowDialog<Customer?>(TopLevel.GetTopLevel(this) as Avalonia.Controls.Window);
            if (result != null) await _vm.UpdateAsync(result);
        };

        DeleteButton.Click += async (_, _) =>
        {
            if (Grid.SelectedItem is Customer c)
                await _vm.DeleteAsync(c);
        };
    }
}
