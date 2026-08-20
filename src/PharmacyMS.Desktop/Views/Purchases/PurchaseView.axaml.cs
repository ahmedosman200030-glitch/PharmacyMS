using System.Globalization;
using Avalonia.Controls;
using PharmacyMS.Desktop.ViewModels;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Desktop.Views.Purchases;

public partial class PurchaseView : UserControl
{
    private readonly PurchaseViewModel _viewModel;

    public PurchaseView() { InitializeComponent(); }

    public PurchaseView(PurchaseViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;

        MedicineGrid.ItemsSource = _viewModel.AvailableMedicines;
        SupplierCombo.ItemsSource = _viewModel.Suppliers;
        LinesGrid.ItemsSource = _viewModel.Lines;
        MedicineGrid.LoadingRow += (_, e) => e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        LinesGrid.LoadingRow += (_, e) => e.Row.Header = (e.Row.GetIndex() + 1).ToString();

        Loaded += async (_, _) => await _viewModel.LoadAsync();

        AddLineButton.Click += (_, _) =>
        {
            if (MedicineGrid.SelectedItem is not Medicine selected) return;
            if (!int.TryParse(QtyBox.Text, out var qty)) return;
            if (!decimal.TryParse(CostBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var cost)) return;

            var batch = string.IsNullOrWhiteSpace(BatchBox.Text) ? null : BatchBox.Text.Trim();
            DateTime? expiry = ExpiryPicker.SelectedDate?.DateTime;

            _viewModel.AddLine(selected, qty, cost, batch, expiry);
            RefreshTotal();

            QtyBox.Text = "1";
            CostBox.Text = "";
            BatchBox.Text = "";
            ExpiryPicker.SelectedDate = null;
        };

        SubmitButton.Click += async (_, _) =>
        {
            StatusText.IsVisible = false;

            if (_viewModel.Lines.Count == 0) return;
            if (SupplierCombo.SelectedItem is not Supplier supplier) return;

            var id = await _viewModel.SubmitAsync(supplier, InvoiceBox.Text);
            RefreshTotal();

            InvoiceBox.Text = "";
            SupplierCombo.SelectedItem = null;

            StatusText.Text = $"Purchase #{id} received successfully.";
            StatusText.IsVisible = true;
        };
    }

    private void RefreshTotal()
    {
        TotalText.Text = $"Total: ${_viewModel.Total:F2}";
    }
}
