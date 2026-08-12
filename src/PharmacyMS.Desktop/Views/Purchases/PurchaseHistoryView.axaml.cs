using Avalonia.Controls;
using PharmacyMS.Desktop.ViewModels;

namespace PharmacyMS.Desktop.Views.Purchases;

public partial class PurchaseHistoryView : UserControl
{
    private readonly PurchaseHistoryViewModel _vm;

    public PurchaseHistoryView() { InitializeComponent(); }

    public PurchaseHistoryView(PurchaseHistoryViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        Grid.ItemsSource = _vm.Purchases;
        Loaded += async (_, _) => await _vm.LoadAsync();
    }
}
