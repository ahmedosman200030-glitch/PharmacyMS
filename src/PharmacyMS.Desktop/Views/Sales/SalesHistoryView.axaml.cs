using Avalonia.Controls;
using PharmacyMS.Desktop.ViewModels;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Desktop.Views.Sales;

public partial class SalesHistoryView : UserControl
{
    private readonly SalesHistoryViewModel _vm;

    public SalesHistoryView() { InitializeComponent(); }
    public SalesHistoryView(SalesHistoryViewModel vm)
    {
        InitializeComponent();
        _vm = vm;

        SalesGrid.ItemsSource = _vm.Sales;

        AttachedToVisualTree += async (_, _) => await _vm.LoadAllAsync();

        ViewAllButton.Click += async (_, _) =>
        {
            InvoiceSearchBox.Text = "";
            await _vm.LoadAllAsync();
        };

        FilterButton.Click += async (_, _) =>
        {
            var from = FromDatePicker.SelectedDate?.DateTime ?? DateTime.Now.AddMonths(-1);
            var to = (ToDatePicker.SelectedDate?.DateTime ?? DateTime.Now).Date.AddDays(1).AddSeconds(-1);
            await _vm.FilterByDateRangeAsync(from, to);
        };

        InvoiceSearchBox.PropertyChanged += async (_, e) =>
        {
            if (e.Property == TextBox.TextProperty)
                await _vm.SearchByInvoiceAsync(InvoiceSearchBox.Text ?? "");
        };

        SalesGrid.AddHandler(Button.ClickEvent, async (object? sender, Avalonia.Interactivity.RoutedEventArgs e) =>
        {
            if (e.Source is not Button btn) return;
            if (btn.Name != "ReprintBtn") return;
            if (btn.DataContext is not Sale sale) return;
            await ReprintAsync(sale);
        }, Avalonia.Interactivity.RoutingStrategies.Bubble);
    }

    private async Task ReprintAsync(Sale sale)
    {
        var receiptService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions
            .GetRequiredService<PharmacyMS.Application.Interfaces.Services.IReceiptService>(PharmacyMS.Desktop.Program.Services);

        var receipt = await receiptService.BuildReceiptAsync(
            sale, sale.CustomerName, sale.PaymentMethod, sale.AmountPaid, sale.ChangeDue, sale.TotalDiscount);

        var win = new ReceiptWindow(receipt, receiptService);
        win.Show();
    }
}
