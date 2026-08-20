using Avalonia.Controls;
using PharmacyMS.Desktop.ViewModels;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Desktop.Views.Purchases;

public partial class ReceiveGoodsView : UserControl
{
    private readonly ReceiveGoodsViewModel _viewModel;

    public ReceiveGoodsView() { InitializeComponent(); }

    public ReceiveGoodsView(ReceiveGoodsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;

        OrdersGrid.ItemsSource = _viewModel.PendingOrders;
        LinesGrid.ItemsSource = _viewModel.Lines;
        OrdersGrid.LoadingRow += (_, e) => e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        LinesGrid.LoadingRow += (_, e) => e.Row.Header = (e.Row.GetIndex() + 1).ToString();

        Loaded += async (_, _) => await _viewModel.LoadAsync();

        OrdersGrid.SelectionChanged += (_, _) =>
        {
            StatusText.IsVisible = false;
            if (OrdersGrid.SelectedItem is PurchaseOrder order)
                _viewModel.SelectOrder(order);
        };

        ConfirmButton.Click += async (_, _) =>
        {
            StatusText.IsVisible = false;
            if (_viewModel.SelectedOrder == null || _viewModel.Lines.Count == 0) return;

            ConfirmButton.IsEnabled = false;
            try
            {
                var notes = string.IsNullOrWhiteSpace(NotesBox.Text) ? null : NotesBox.Text.Trim();
                var id = await _viewModel.SubmitAsync(notes);

                NotesBox.Text = "";
                StatusText.Text = $"Goods Receipt #{id} recorded. Pending admin approval.";
                StatusText.IsVisible = true;
            }
            finally
            {
                ConfirmButton.IsEnabled = true;
            }
        };
    }
}
