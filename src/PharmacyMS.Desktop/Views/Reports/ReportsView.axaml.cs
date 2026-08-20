using Avalonia.Controls;
using Avalonia.Platform.Storage;
using PharmacyMS.Desktop.Services;
using PharmacyMS.Desktop.ViewModels;
using PharmacyMS.Application.Interfaces.Services;

namespace PharmacyMS.Desktop.Views.Reports;

public partial class ReportsView : UserControl
{
    private readonly ReportsViewModel _vm;
    private readonly IBrandingService _brandingService;
    private DateTime _from, _to;
    private DateTime _reconMonthStart, _reconMonthEnd;

    public ReportsView() { InitializeComponent(); }
    public ReportsView(ReportsViewModel vm, IBrandingService brandingService)
    {
        InitializeComponent();
        _vm = vm;
        _brandingService = brandingService;

        FromBox.Text = DateTime.Now.AddMonths(-1).ToString("yyyy-MM-dd");
        ToBox.Text   = DateTime.Now.ToString("yyyy-MM-dd");
        ReconMonthBox.Text = DateTime.Now.ToString("yyyy-MM");

        AttachedToVisualTree += async (_, _) => { await RunReport(); await RunReconciliation(); await LoadInventory(); await LoadSuppliers(); };

        RunButton.Click           += async (_, _) => await RunReport();
        ExportPdfButton.Click     += async (_, _) => await ExportPdf();
        ExportExcelButton.Click   += async (_, _) => await ExportExcel();
        ReconRunButton.Click      += async (_, _) => await RunReconciliation();
        ReconExportButton.Click   += async (_, _) => await ExportReconciliationExcel();
        ReconExportPdfButton.Click+= async (_, _) => await ExportReconciliationPdf();
        LoadInventoryBtn.Click    += async (_, _) => await LoadInventory();
        LoadSuppliersBtn.Click    += async (_, _) => await LoadSuppliers();

        TopGrid.LoadingRow      += (_, e) => e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        ReconGrid.LoadingRow    += (_, e) => e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        DailySalesGrid.LoadingRow += (_, e) => e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        PnLGrid.LoadingRow       += (_, e) => e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        InventoryGrid.LoadingRow += (_, e) => e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        PaymentGrid.LoadingRow   += (_, e) => e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        TaxGrid.LoadingRow       += (_, e) => e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        SupplierGrid.LoadingRow  += (_, e) => e.Row.Header = (e.Row.GetIndex() + 1).ToString();
    }

    private async Task RunReport()
    {
        if (!DateTime.TryParse(FromBox.Text, out _from)) _from = DateTime.Now.AddMonths(-1);
        if (!DateTime.TryParse(ToBox.Text,   out _to))   _to   = DateTime.Now;
        await _vm.LoadAsync(_from, _to);

        RevenueText.Text  = $"${_vm.TotalRevenue:F2}";
        CostText.Text     = $"${_vm.TotalPurchaseCost:F2}";
        ProfitText.Text   = $"${_vm.NetProfit:F2}";
        TaxText.Text      = $"${_vm.TotalTax:F2}";
        DiscountText.Text = $"${_vm.TotalDiscount:F2}";
        TxText.Text       = _vm.TotalTransactions.ToString();

        TopGrid.ItemsSource        = _vm.TopSellers;
        DailySalesGrid.ItemsSource = _vm.DailySales;
        PnLGrid.ItemsSource        = _vm.PurchaseVsSales;
        PaymentGrid.ItemsSource    = _vm.PaymentBreakdown;
        TaxGrid.ItemsSource        = _vm.TaxReport;
    }

    private async Task LoadInventory()
    {
        await _vm.LoadInventoryValuationAsync();
        InventoryGrid.ItemsSource = _vm.InventoryValuation;
        CostValueText.Text   = $"${_vm.TotalInventoryCostValue:F2}";
        RetailValueText.Text = $"${_vm.TotalInventoryRetailValue:F2}";
    }

    private async Task LoadSuppliers()
    {
        await _vm.LoadSupplierPaymentsAsync();
        SupplierGrid.ItemsSource = _vm.SupplierPayments;
    }

    private async Task RunReconciliation()
    {
        if (!DateTime.TryParseExact(ReconMonthBox.Text?.Trim(), "yyyy-MM",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var monthDate))
            monthDate = DateTime.Now;
        _reconMonthStart = new DateTime(monthDate.Year, monthDate.Month, 1);
        _reconMonthEnd   = _reconMonthStart.AddMonths(1).AddSeconds(-1);
        await _vm.LoadStockReconciliationAsync(_reconMonthStart, _reconMonthEnd);
        ReconGrid.ItemsSource = _vm.StockReconciliation;
    }

    private async Task ExportReconciliationExcel()
    {
        var file = await PickSaveFile("Excel File", "xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        if (file is null) return;
        var branding = await _brandingService.GetAsync();
        ReportExportService.ExportStockReconciliationExcel(file.TryGetLocalPath()!, _reconMonthStart, _reconMonthEnd, _vm.StockReconciliation, branding);
    }

    private async Task ExportReconciliationPdf()
    {
        var file = await PickSaveFile("PDF File", "pdf", "application/pdf");
        if (file is null) return;
        var branding = await _brandingService.GetAsync();
        ReportExportService.ExportStockReconciliationPdf(file.TryGetLocalPath()!, _reconMonthStart, _reconMonthEnd, _vm.StockReconciliation, branding);
    }

    private async Task ExportExcel()
    {
        var file = await PickSaveFile("Excel File", "xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        if (file is null) return;
        var branding = await _brandingService.GetAsync();
        ReportExportService.ExportExcel(file.TryGetLocalPath()!, _from, _to,
            _vm.TotalRevenue, _vm.TotalPurchaseCost, _vm.NetProfit, _vm.TotalTransactions,
            _vm.DailySales, _vm.PurchaseVsSales, _vm.PaymentBreakdown, _vm.TaxReport,
            _vm.InventoryValuation, _vm.SupplierPayments, _vm.TopSellers, branding);
    }

    private async Task ExportPdf()
    {
        var file = await PickSaveFile("PDF File", "pdf", "application/pdf");
        if (file is null) return;
        var branding = await _brandingService.GetAsync();
        ReportExportService.ExportPdf(file.TryGetLocalPath()!, _from, _to,
            _vm.TotalRevenue, _vm.TotalPurchaseCost, _vm.NetProfit, _vm.TotalTransactions,
            _vm.DailySales, _vm.PurchaseVsSales, _vm.PaymentBreakdown, _vm.TaxReport,
            _vm.InventoryValuation, _vm.SupplierPayments, _vm.TopSellers, branding);
    }

    private async Task<IStorageFile?> PickSaveFile(string name, string ext, string mime)
    {
        var topLevel = TopLevel.GetTopLevel(this)!;
        return await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = $"Save {name}",
            SuggestedFileName = $"pharmacy-report-{DateTime.Now:yyyy-MM-dd}",
            DefaultExtension = ext,
            FileTypeChoices = [new FilePickerFileType(name) { Patterns = [$"*.{ext}"], MimeTypes = [mime] }]
        });
    }
}
