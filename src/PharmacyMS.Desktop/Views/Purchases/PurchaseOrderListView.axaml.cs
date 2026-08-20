using Avalonia.Controls;
using PharmacyMS.Desktop.ViewModels;

namespace PharmacyMS.Desktop.Views.Purchases;

public partial class PurchaseOrderListView : UserControl
{
    private readonly PurchaseOrderListViewModel _viewModel;

    public PurchaseOrderListView() { InitializeComponent(); }

    public PurchaseOrderListView(PurchaseOrderListViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;

        Grid.ItemsSource = _viewModel.Orders;
        Grid.LoadingRow += (_, e) => e.Row.Header = (e.Row.GetIndex() + 1).ToString();

        Loaded += async (_, _) => await _viewModel.LoadAsync();
    }
}
