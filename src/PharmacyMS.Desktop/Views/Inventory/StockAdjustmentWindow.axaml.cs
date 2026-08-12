using Avalonia.Controls;
using PharmacyMS.Desktop.ViewModels;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Desktop.Views.Inventory;

public partial class StockAdjustmentWindow : Window
{
    private readonly StockAdjustmentViewModel _viewModel;

    public StockAdjustmentWindow() { InitializeComponent(); }
    public StockAdjustmentWindow(StockAdjustmentViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;

        HistoryGrid.ItemsSource = _viewModel.RecentAdjustments;

        Opened += async (_, _) => await _viewModel.LoadAsync();

        MedicineBox.SelectionChanged += (_, _) =>
        {
            if (MedicineBox.SelectedItem is Medicine m)
                CurrentStockText.Text = $"Current stock: {m.QuantityInStock}";
            else
                CurrentStockText.Text = "Current stock: —";
        };

        SubmitButton.Click += async (_, _) => await Submit();
    }

    private async Task Submit()
    {
        ErrorText.IsVisible = false;

        if (MedicineBox.SelectedItem is not Medicine medicine)
        {
            ShowError("Select a medicine.");
            return;
        }

        if (!int.TryParse(QtyBox.Text, out var qty) || qty <= 0)
        {
            ShowError("Enter a quantity greater than zero.");
            return;
        }

        if (ReasonBox.SelectedItem is not ComboBoxItem reasonItem)
        {
            ShowError("Select a reason.");
            return;
        }

        var reason = reasonItem.Content?.ToString() ?? "Other";
        if (!string.IsNullOrWhiteSpace(NotesBox.Text))
            reason += $" — {NotesBox.Text.Trim()}";

        var signedQty = RemoveRadio.IsChecked == true ? -qty : qty;

        try
        {
            await _viewModel.SubmitAsync(medicine, signedQty, reason);
        }
        catch (InvalidOperationException ex)
        {
            ShowError(ex.Message);
            return;
        }

        // Reset form
        QtyBox.Text = string.Empty;
        NotesBox.Text = string.Empty;
        ReasonBox.SelectedIndex = -1;
        CurrentStockText.Text = "Current stock: —";
        MedicineBox.SelectedItem = null;
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.IsVisible = true;
    }
}
