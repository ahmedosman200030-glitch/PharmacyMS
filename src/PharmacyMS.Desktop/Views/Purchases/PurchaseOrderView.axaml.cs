using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;
using PharmacyMS.Desktop.ViewModels;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Desktop.Views.Purchases;

public partial class PurchaseOrderView : UserControl
{
    private readonly PurchaseOrderViewModel _viewModel;

    public PurchaseOrderView() { InitializeComponent(); }

    public PurchaseOrderView(PurchaseOrderViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;

        MedicineGrid.ItemsSource = _viewModel.AvailableMedicines;
        SupplierCombo.ItemsSource = _viewModel.Suppliers;
        LinesGrid.ItemsSource = _viewModel.Lines;
        MedicineGrid.LoadingRow += (_, e) => e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        LinesGrid.LoadingRow += (_, e) => e.Row.Header = (e.Row.GetIndex() + 1).ToString();

        Loaded += async (_, _) => await _viewModel.LoadAsync();

        var units = new List<string>
        {
            "Box", "Bottle", "Packet", "Pcs", "Vial", "Ampoule",
            "Sachet", "Strip", "Tube", "Syringe", "Roll", "Other"
        };
        UnitBox.ItemsSource = units;
        UnitBox.SelectedItem = "Box";

        MedicineGrid.SelectionChanged += (_, _) =>
        {
            if (MedicineGrid.SelectedItem is Medicine sel)
                UnitBox.SelectedItem = sel.Unit ?? "Box";
        };

        AddLineButton.Click += (_, _) =>
        {
            if (MedicineGrid.SelectedItem is not Medicine selected) return;
            if (!int.TryParse(QtyBox.Text, out var qty)) return;
            if (!decimal.TryParse(CostBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var cost)) return;

            var unit = (UnitBox.SelectedItem as string) ?? "Box";
            _viewModel.AddLine(selected, qty, cost, unit);
            RefreshTotal();

            QtyBox.Text = "1";
            CostBox.Text = "";
        };

        SaveDraftButton.Click += async (_, _) => await SubmitAsync(sendNow: false);
        SendButton.Click += async (_, _) => await SubmitAsync(sendNow: true);
    }

    private void RemoveLineButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: PurchaseOrderLine line }) return;
        _viewModel.RemoveLine(line);
        RefreshTotal();
    }

    private async Task SubmitAsync(bool sendNow)
    {
        StatusText.IsVisible = false;

        if (_viewModel.Lines.Count == 0) return;
        if (SupplierCombo.SelectedItem is not Supplier supplier) return;

        DateTime? expected = ExpectedDatePicker.SelectedDate?.DateTime;
        var notes = string.IsNullOrWhiteSpace(NotesBox.Text) ? null : NotesBox.Text.Trim();

        var id = await _viewModel.SubmitAsync(supplier, expected, notes, sendNow);
        RefreshTotal();

        SupplierCombo.SelectedItem = null;
        NotesBox.Text = "";
        ExpectedDatePicker.SelectedDate = null;

        if (sendNow && _viewModel.LastSubmittedOrder != null)
        {
            try
            {
                var pdfPath = await _viewModel.GeneratePdfAsync(_viewModel.LastSubmittedOrder, supplier);
                Process.Start(new ProcessStartInfo(pdfPath) { UseShellExecute = true });
                StatusText.Text = $"Purchase Order #{id} sent to supplier. PDF saved and opened.";
            }
            catch (Exception ex)
            {
                var logPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "pdf_error.txt");
                System.IO.File.WriteAllText(logPath, ex.ToString());
                StatusText.Text = $"PDF failed — see pdf_error.txt on Desktop";
            }
        }
        else
        {
            StatusText.Text = $"Purchase Order #{id} saved as draft.";
        }
        StatusText.IsVisible = true;
    }

    private void RefreshTotal()
    {
        TotalText.Text = $"Total: ${_viewModel.Total:F2}";
    }
}
