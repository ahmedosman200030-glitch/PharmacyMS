using Avalonia.Controls;
using PharmacyMS.Desktop.ViewModels;

namespace PharmacyMS.Desktop.Views.Sales;

public partial class CreditSalesView : UserControl
{
    private readonly CreditSalesViewModel _vm;

    public CreditSalesView() { InitializeComponent(); }
    public CreditSalesView(CreditSalesViewModel vm)
    {
        InitializeComponent();
        _vm = vm;

        CreditSalesGrid.ItemsSource = _vm.CreditSales;

        AttachedToVisualTree += async (_, _) =>
        {
            await RefreshAsync();
        };

        CreditSalesGrid.AddHandler(Button.ClickEvent, async (object? sender, Avalonia.Interactivity.RoutedEventArgs e) =>
        {
            if (e.Source is not Button btn) return;
            if (btn.Name != "RecordPaymentBtn") return;
            if (btn.DataContext is not CreditSaleRow row) return;

            var owner = TopLevel.GetTopLevel(this) as Window;
            if (owner == null) return;

            var amount = await PharmacyMS.Desktop.Views.Shared.PaymentDialog.ShowAsync(owner, row.CustomerName, row.Balance);
            if (amount == null) return;

            await _vm.RecordPaymentAsync(row, amount.Value);
            await RefreshAsync();
            StatusText.Text = $"Recorded ${amount:F2} payment from {row.CustomerName}.";
        }, Avalonia.Interactivity.RoutingStrategies.Bubble);
    }

    private async Task RefreshAsync()
    {
        await _vm.LoadAsync();
        TotalOutstandingText.Text = $"${_vm.TotalOutstanding:F2}";
    }
}
