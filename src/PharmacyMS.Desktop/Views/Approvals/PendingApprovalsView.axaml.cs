using Avalonia.Controls;
using Avalonia.Interactivity;
using PharmacyMS.Desktop.ViewModels;
using PharmacyMS.Domain.Entities;
using System.Linq;

namespace PharmacyMS.Desktop.Views.Approvals;

public partial class PendingApprovalsView : UserControl
{
    private readonly PendingApprovalsViewModel _vm;

    public PendingApprovalsView() { InitializeComponent(); }

    public PendingApprovalsView(PendingApprovalsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        CustomersGrid.ItemsSource = _vm.PendingCustomers;
        SuppliersGrid.ItemsSource = _vm.PendingSuppliers;
        PurchasesGrid.ItemsSource = _vm.PendingPurchases;
        ReceiptsGrid.ItemsSource = _vm.PendingReceipts;
        PaymentsGrid.ItemsSource = _vm.PendingPayments;
        ExpensesGrid.ItemsSource = _vm.PendingExpenses;

        AttachedToVisualTree += async (_, _) =>
        {
            await _vm.LoadAsync();
            RefreshEmpty();
        };
    }

    private void RefreshEmpty()
    {
        NoCustomersText.IsVisible = _vm.PendingCustomers.Count == 0;
        NoSuppliersText.IsVisible = _vm.PendingSuppliers.Count == 0;
        NoPurchasesText.IsVisible = _vm.PendingPurchases.Count == 0;
        NoReceiptsText.IsVisible = _vm.PendingReceipts.Count == 0;
        NoPaymentsText.IsVisible = _vm.PendingPayments.Count == 0;
        NoExpensesText.IsVisible = _vm.PendingExpenses.Count == 0;
    }

    public async void ApproveCustomer_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Customer c }) { await _vm.ApproveCustomerAsync(c); RefreshEmpty(); }
    }
    public async void RejectCustomer_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Customer c }) { await _vm.RejectCustomerAsync(c); RefreshEmpty(); }
    }
    public async void ApproveSupplier_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Supplier s }) { await _vm.ApproveSupplierAsync(s); RefreshEmpty(); }
    }
    public async void RejectSupplier_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Supplier s }) { await _vm.RejectSupplierAsync(s); RefreshEmpty(); }
    }
    public async void ApprovePurchase_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Purchase p }) { await _vm.ApprovePurchaseAsync(p); RefreshEmpty(); }
    }
    public async void RejectPurchase_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Purchase p }) { await _vm.RejectPurchaseAsync(p); RefreshEmpty(); }
    }
    public async void ApproveReceipt_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: GoodsReceipt r }) { await _vm.ApproveReceiptAsync(r); RefreshEmpty(); }
    }
    public async void RejectReceipt_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: GoodsReceipt r })
        {
            var owner = TopLevel.GetTopLevel(this) as Window;
            if (owner == null) return;
            var reason = await RejectReasonDialog.ShowAsync(owner, $"Receipt for PO #{r.PurchaseOrderId}");
            if (string.IsNullOrEmpty(reason)) return;
            await _vm.RejectReceiptAsync(r, reason);
            RefreshEmpty();
        }
    }
    public async void ViewReceipt_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: GoodsReceipt r })
        {
            var owner = TopLevel.GetTopLevel(this) as Window;
            if (owner == null) return;
            await GoodsReceiptDetailDialog.ShowAsync(owner, r);
        }
    }
    public async void ApprovePayment_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: PendingSalePayment pay }) { await _vm.ApprovePaymentAsync(pay); RefreshEmpty(); }
    }
    public async void RejectPayment_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: PendingSalePayment pay })
        {
            var owner = TopLevel.GetTopLevel(this) as Window;
            if (owner == null) return;
            var reason = await RejectReasonDialog.ShowAsync(owner, $"Payment for Sale #{pay.SaleId}");
            if (string.IsNullOrEmpty(reason)) return;
            await _vm.RejectPaymentAsync(pay, reason);
            RefreshEmpty();
        }
    }
    public async void ApproveExpense_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: PendingExpense ex }) { await _vm.ApproveExpenseAsync(ex); RefreshEmpty(); }
    }
    public async void RejectExpense_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: PendingExpense ex })
        {
            var owner = TopLevel.GetTopLevel(this) as Window;
            if (owner == null) return;
            var reason = await RejectReasonDialog.ShowAsync(owner, $"Expense: {ex.Description}");
            if (string.IsNullOrEmpty(reason)) return;
            await _vm.RejectExpenseAsync(ex, reason);
            RefreshEmpty();
        }
    }
}
