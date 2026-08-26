using Avalonia.Controls;
using PharmacyMS.Desktop.ViewModels;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Desktop.Views.Sales;

public partial class SalesReturnView : UserControl
{
    private readonly SalesReturnViewModel _viewModel;
    private Sale? _selectedSale;
    private SaleItem? _selectedItem;

    public SalesReturnView() { InitializeComponent(); }
    public SalesReturnView(SalesReturnViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        HistoryGrid.ItemsSource = _viewModel.RecentReturns;
        HistoryGrid.LoadingRow += (_, e) => e.Row.Header = (e.Row.GetIndex() + 1).ToString();

        AttachedToVisualTree += async (_, _) => await _viewModel.LoadAsync();

        SearchBox.TextChanged += (_, _) => FilterSales();

        SalesList.SelectionChanged += (_, _) =>
        {
            if (SalesList.SelectedItem is Sale sale)
                SelectSale(sale);
        };

        BackButton.Click += (_, _) => ShowSalePicker();

        ItemBox.SelectionChanged += (_, _) =>
        {
            if (ItemBox.SelectedItem is SaleItem item)
            {
                _selectedItem = item;
                SoldQtyText.Text = $"Sold: {item.Quantity} × ${item.UnitPrice:F2}";
                PriceBox.Text = item.UnitPrice.ToString("F2");
            }
            UpdateRefundPreview();
        };
        QtyBox.TextChanged += (_, _) => UpdateRefundPreview();
        PriceBox.TextChanged += (_, _) => UpdateRefundPreview();

        SubmitButton.Click += async (_, _) => await Submit();
    }

    private void FilterSales()
    {
        var term = SearchBox.Text?.Trim().ToLowerInvariant() ?? "";
        if (string.IsNullOrEmpty(term))
        {
            SalesList.ItemsSource = _viewModel.RecentSales;
            return;
        }
        SalesList.ItemsSource = _viewModel.RecentSales
            .Where(s => s.InvoiceNumber.ToLowerInvariant().Contains(term)
                     || s.CustomerName.ToLowerInvariant().Contains(term))
            .ToList();
    }

    private void SelectSale(Sale sale)
    {
        _selectedSale = sale;
        _selectedItem = null;

        SelectedInvoiceText.Text = $"Invoice {sale.InvoiceNumber}";
        SelectedCustomerText.Text = $"{sale.CustomerName} — {sale.CreatedAt}";

        ItemBox.ItemsSource = sale.Items;
        ItemBox.DisplayMemberBinding = new Avalonia.Data.Binding("MedicineName");
        ItemBox.SelectedItem = null;
        SoldQtyText.Text = "";
        QtyBox.Text = string.Empty;
        PriceBox.Text = string.Empty;
        ReasonBox.SelectedIndex = -1;
        NotesBox.Text = string.Empty;
        ErrorText.IsVisible = false;
        RefundPreviewText.Text = "Refund total: $0.00";

        SalePickerPanel.IsVisible = false;
        ReturnFormScroll.IsVisible = true;
    }

    private void ShowSalePicker()
    {
        SalePickerPanel.IsVisible = true;
        ReturnFormScroll.IsVisible = false;
        _selectedSale = null;
        _selectedItem = null;
    }

    private void UpdateRefundPreview()
    {
        if (int.TryParse(QtyBox.Text, out var qty) && decimal.TryParse(PriceBox.Text, out var price))
            RefundPreviewText.Text = $"Refund total: ${qty * price:F2}";
        else
            RefundPreviewText.Text = "Refund total: $0.00";
    }

    private async Task Submit()
    {
        ErrorText.IsVisible = false;

        if (_selectedSale == null)
        {
            ShowError("Select a sale first.");
            return;
        }

        if (_selectedItem == null)
        {
            ShowError("Select an item from this invoice.");
            return;
        }

        if (!int.TryParse(QtyBox.Text, out var qty) || qty <= 0)
        {
            ShowError("Enter a quantity greater than zero.");
            return;
        }

        if (!decimal.TryParse(PriceBox.Text, out var price) || price < 0)
        {
            ShowError("Enter a valid unit price.");
            return;
        }

        if (ReasonBox.SelectedItem is not ComboBoxItem reasonItem)
        {
            ShowError("Select a reason.");
            return;
        }

        var paymentMethod = (PaymentMethodBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Cash";
        var reason = reasonItem.Content?.ToString() ?? "Other";
        if (!string.IsNullOrWhiteSpace(NotesBox.Text))
            reason += $" — {NotesBox.Text.Trim()}";

        try
        {
            await _viewModel.SubmitAsync(_selectedSale, _selectedItem, qty, price, paymentMethod, reason);
        }
        catch (InvalidOperationException ex)
        {
            ShowError(ex.Message);
            return;
        }

        ShowSalePicker();
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.IsVisible = true;
    }
}
