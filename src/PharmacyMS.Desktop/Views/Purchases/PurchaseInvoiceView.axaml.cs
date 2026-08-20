using Avalonia.Controls;
using Avalonia.Interactivity;
using PharmacyMS.Desktop.ViewModels;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Desktop.Views.Purchases;

public partial class PurchaseInvoiceView : UserControl
{
    private readonly PurchaseInvoiceViewModel _vm;

    public PurchaseInvoiceView() { InitializeComponent(); }

    public PurchaseInvoiceView(PurchaseInvoiceViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        ListGrid.ItemsSource = _vm.Purchases;
        ListGrid.LoadingRow += (_, e) => e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        ItemsGrid.LoadingRow += (_, e) => e.Row.Header = (e.Row.GetIndex() + 1).ToString();

        Loaded += async (_, _) =>
        {
            await _vm.LoadAsync();
            RefreshStats();
        };

        SearchBox.TextChanged += (_, _) => _vm.ApplyFilter(SearchBox.Text);

        ListGrid.SelectionChanged += async (_, _) =>
        {
            if (ListGrid.SelectedItem is not Purchase selected) return;
            await ShowDetailAsync(selected.Id);
        };

        ListGrid.AddHandler(Button.ClickEvent, async (object? sender, RoutedEventArgs e) =>
        {
            if (e.Source is Button { DataContext: Purchase p })
                await ShowDetailAsync(p.Id);
        });

        RecordPaymentButton.Click += async (_, _) =>
        {
            if (_vm.Selected == null) return;

            var owner = TopLevel.GetTopLevel(this) as Window;
            var dialog = new PurchasePaymentDialog(_vm.Selected);
            await dialog.ShowDialog(owner!);

            if (dialog.ResultAmount is decimal amount)
            {
                var purchaseId = _vm.Selected.Id;
                await _vm.RecordPaymentAsync(purchaseId, amount);
                RefreshStats();
                await ShowDetailAsync(purchaseId);
            }
        };
    }

    public async void ApproveButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: PharmacyMS.Domain.Entities.Purchase purchase })
        {
            await _vm.ApproveAsync(purchase);
            RefreshStats();
        }
    }

    public async void RejectButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: PharmacyMS.Domain.Entities.Purchase purchase })
        {
            await _vm.RejectAsync(purchase);
            RefreshStats();
        }
    }

    private async Task ShowDetailAsync(int purchaseId)
    {
        var detail = await _vm.LoadDetailAsync(purchaseId);
        if (detail == null) return;

        InvoiceTitleText.Text = $"Invoice #{detail.Id}";
        InvoiceSupplierText.Text = $"Supplier: {detail.SupplierName}";
        InvoiceDateText.Text = $"Date: {detail.CreatedAt:yyyy-MM-dd HH:mm}";
        InvoiceNumberText.Text = string.IsNullOrWhiteSpace(detail.InvoiceNumber)
            ? "Invoice #: (none)"
            : $"Invoice #: {detail.InvoiceNumber}";
        InvoiceStatusText.Text = $"Status: {detail.Status}";
        InvoiceTotalText.Text = $"Total: ${detail.TotalAmount:F2}";
        InvoicePaidText.Text = $"Paid: ${detail.AmountPaid:F2}";
        InvoiceDueText.Text = $"Due: ${detail.DueAmount:F2}";
        ItemsGrid.ItemsSource = detail.Items;

        RecordPaymentButton.IsEnabled = detail.DueAmount > 0;

        EmptyState.IsVisible = false;
        DetailPanel.IsVisible = true;
    }

    private void RefreshStats()
    {
        TotalInvoicesText.Text = _vm.TotalInvoices.ToString();
        TotalAmountText.Text = $"${_vm.TotalAmountSum:F2}";
        PaidAmountText.Text = $"${_vm.PaidAmountSum:F2}";
        DueAmountText.Text = $"${_vm.DueAmountSum:F2}";
    }
}
