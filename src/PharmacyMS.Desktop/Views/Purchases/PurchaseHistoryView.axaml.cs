using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using PharmacyMS.Desktop.ViewModels;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Desktop.Views.Purchases;

public partial class PurchaseHistoryView : UserControl
{
    private readonly PurchaseHistoryViewModel _vm = null!;
    private List<Purchase> _all = new();

    public PurchaseHistoryView() { InitializeComponent(); }

    public PurchaseHistoryView(PurchaseHistoryViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        HistoryGrid.LoadingRow += (_, e) => e.Row.Header = (e.Row.Index + 1).ToString();

        SearchBox.TextChanged += (_, _) => ApplyLocalFilter(SearchBox.Text);

        Loaded += async (_, _) =>
        {
            await _vm.LoadAsync();
            _all = _vm.Purchases.ToList();
            HistoryGrid.ItemsSource = _all;
            RefreshStats();
        };
    }

    public async void ViewButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: Purchase row }) return;

        var detail = await _vm.LoadDetailAsync(row.Id);
        if (detail == null) return;

        var owner = TopLevel.GetTopLevel(this) as Window;
        var dialog = new PurchaseBillDialog(detail);
        await dialog.ShowDialog(owner!);
    }

    private void ApplyLocalFilter(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            HistoryGrid.ItemsSource = _all;
            return;
        }

        var q = query.Trim();
        HistoryGrid.ItemsSource = _all
            .Where(p =>
                (p.SupplierName?.Contains(q, System.StringComparison.OrdinalIgnoreCase) ?? false) ||
                (p.InvoiceNumber?.Contains(q, System.StringComparison.OrdinalIgnoreCase) ?? false))
            .ToList();
    }

    private void RefreshStats()
    {
        TotalBillsText.Text = _all.Count.ToString();
        TotalAmountText.Text = $"${_all.Sum(p => p.TotalAmount):F2}";
        PaidAmountText.Text = $"${_all.Sum(p => p.AmountPaid):F2}";
        DueAmountText.Text = $"${_all.Sum(p => p.DueAmount):F2}";
    }
}
