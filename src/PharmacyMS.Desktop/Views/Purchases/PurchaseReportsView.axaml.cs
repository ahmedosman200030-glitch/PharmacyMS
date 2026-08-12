using Avalonia.Controls;
using PharmacyMS.Desktop.ViewModels;

namespace PharmacyMS.Desktop.Views.Purchases;

public partial class PurchaseReportsView : UserControl
{
    private readonly PurchaseReportsViewModel _vm;

    public PurchaseReportsView() { InitializeComponent(); }

    public PurchaseReportsView(PurchaseReportsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        SupplierGrid.ItemsSource = _vm.BySupplier;
        MedicineGrid.ItemsSource = _vm.ByMedicine;

        Loaded += async (_, _) =>
        {
            await _vm.LoadAsync();
            TotalSpendText.Text = $"${_vm.TotalSpend:F2}";
        };
    }
}
