using Avalonia.Controls;
using PharmacyMS.Desktop.ViewModels;

namespace PharmacyMS.Desktop.Views.Approvals;

public partial class MySubmissionsView : UserControl
{
    private readonly MySubmissionsViewModel _vm;

    public MySubmissionsView() { InitializeComponent(); }

    public MySubmissionsView(MySubmissionsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        CustomersGrid.ItemsSource = _vm.MyCustomers;
        SuppliersGrid.ItemsSource = _vm.MySuppliers;
        PaymentsGrid.ItemsSource = _vm.MyPayments;
        ExpensesGrid.ItemsSource = _vm.MyExpenses;
        ReceiptsGrid.ItemsSource = _vm.MyReceipts;

        AttachedToVisualTree += async (_, _) =>
        {
            await _vm.LoadAsync();
            RefreshEmpty();
        };
    }

    private void RefreshEmpty()
    {
        NoCustomersText.IsVisible = _vm.MyCustomers.Count == 0;
        NoSuppliersText.IsVisible = _vm.MySuppliers.Count == 0;
        NoPaymentsText.IsVisible = _vm.MyPayments.Count == 0;
        NoExpensesText.IsVisible = _vm.MyExpenses.Count == 0;
        NoReceiptsText.IsVisible = _vm.MyReceipts.Count == 0;
    }
}
