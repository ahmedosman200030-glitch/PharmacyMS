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

    public ReportsView() { InitializeComponent(); }
    public ReportsView(ReportsViewModel vm, IBrandingService brandingService)
    {
        InitializeComponent();
        _vm = vm;
        _brandingService = brandingService;
        TopGrid.ItemsSource = _vm.TopSellers;
        FromBox.Text = DateTime.Now.AddMonths(-1).ToString("yyyy-MM-dd");
        ToBox.Text   = DateTime.Now.ToString("yyyy-MM-dd");

        ReconGrid.ItemsSource = _vm.StockReconciliation;
        ReconMonthBox.Text = DateTime.Now.ToString("yyyy-MM");

        AttachedToVisualTree  += async (_, _) => { await RunReport(); await RunReconciliation(); };
        RunButton.Click        += async (_, _) => await RunReport();
        ExportExcelButton.Click += async (_, _) => await ExportExcel();
        ExportPdfButton.Click   += async (_, _) => await ExportPdf();
        ReconRunButton.Click     += async (_, _) => await RunReconciliation();
        ReconExportButton.Click  += async (_, _) => await ExportReconciliationExcel();
        ReconExportPdfButton.Click += async (_, _) => await ExportReconciliationPdf();
    }

    private DateTime _reconMonthStart, _reconMonthEnd;

    private async Task RunReconciliation()
    {
        if (!DateTime.TryParseExact(ReconMonthBox.Text?.Trim(), "yyyy-MM",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var monthDate))
        {
            monthDate = DateTime.Now;
        }
        _reconMonthStart = new DateTime(monthDate.Year, monthDate.Month, 1);
        _reconMonthEnd = _reconMonthStart.AddMonths(1).AddSeconds(-1);
        await _vm.LoadStockReconciliationAsync(_reconMonthStart, _reconMonthEnd);
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

    private async Task RunReport()
    {
        if (!DateTime.TryParse(FromBox.Text, out _from)) _from = DateTime.Now.AddMonths(-1);
        if (!DateTime.TryParse(ToBox.Text,   out _to))   _to   = DateTime.Now;
        await _vm.LoadAsync(_from, _to);
        RevenueText.Text = $"${_vm.TotalRevenue:F2}";
        CostText.Text    = $"${_vm.TotalPurchaseCost:F2}";
        ProfitText.Text  = $"${_vm.NetProfit:F2}";
        TxText.Text      = _vm.TotalTransactions.ToString();
    }

    private async Task ExportExcel()
    {
        var file = await PickSaveFile("Excel File", "xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        if (file is null) return;
        var branding = await _brandingService.GetAsync();
        ReportExportService.ExportExcel(file.TryGetLocalPath()!, _from, _to,
            _vm.TotalRevenue, _vm.TotalPurchaseCost, _vm.NetProfit,
            _vm.TotalTransactions, _vm.TopSellers, branding);
    }

    private async Task ExportPdf()
    {
        var file = await PickSaveFile("PDF File", "pdf", "application/pdf");
        if (file is null) return;
        var branding = await _brandingService.GetAsync();
        ReportExportService.ExportPdf(file.TryGetLocalPath()!, _from, _to,
            _vm.TotalRevenue, _vm.TotalPurchaseCost, _vm.NetProfit,
            _vm.TotalTransactions, _vm.TopSellers, branding);
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
