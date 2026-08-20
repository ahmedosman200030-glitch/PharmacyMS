using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using PharmacyMS.Application.Services;
using PharmacyMS.Desktop.ViewModels;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Desktop.Views.Accounting;

public partial class AccountingView : UserControl
{
    private readonly AccountingViewModel _vm;

    public AccountingView() { InitializeComponent(); }

    private readonly int _initialTab;

    public AccountingView(AccountingViewModel vm, int initialTab = 0)
    {
        InitializeComponent();
        _vm = vm;
        _initialTab = initialTab;

        RangePicker.SetRange(_vm.FromDate, _vm.ToDate);

        TxnGrid.ItemsSource = _vm.RecentTransactions;

        RefreshButton.Click += async (_, _) =>
        {
            _vm.FromDate = RangePicker.FromDate;
            _vm.ToDate = RangePicker.ToDate;
            await _vm.LoadAsync();
            RefreshStats();
            DrawChart();
        };

        ExportBtn.Click += (_, _) => ExportPL();

        AttachedToVisualTree += async (_, _) =>
        {
            await _vm.LoadAsync();
            RefreshStats();
            DrawChart();
        };

        ChartCanvas.SizeChanged += (_, _) => DrawChart();
    }

    private void RefreshStats()
    {
        RevenueText.Text = $"${_vm.TotalRevenue:F2}";
        PurchasesText.Text = $"${_vm.TotalPurchases:F2}";
        GrossProfitText.Text = $"${_vm.GrossProfit:F2}";
        GrossProfitText.Foreground = _vm.GrossProfit >= 0 ? Brushes.DarkGreen : Brushes.Crimson;
        ExpensesText.Text = $"${_vm.TotalExpenses:F2}";
        NetProfitText.Text = $"${_vm.NetProfit:F2}";
        NetProfitText.Foreground = _vm.NetProfit >= 0 ? Brushes.DarkGreen : Brushes.Crimson;
        CashBalanceText.Text = $"${_vm.CashBalance:F2}";
        CustomerCreditText.Text = $"${_vm.CustomerCredit:F2}";
        SupplierPayablesText.Text = $"${_vm.SupplierPayables:F2}";
        StockValueText.Text = $"${_vm.StockValue:F2}";
    }

    private void DrawChart()
    {
        var canvas = ChartCanvas;
        canvas.Children.Clear();
        var pts = _vm.ChartPoints;
        if (pts == null || pts.Count == 0) return;

        double w = canvas.Bounds.Width > 0 ? canvas.Bounds.Width : 600;
        double h = canvas.Bounds.Height > 0 ? canvas.Bounds.Height : 220;
        double padL = 50, padR = 20, padT = 10, padB = 40;
        double chartW = w - padL - padR;
        double chartH = h - padT - padB;

        var allVals = pts.SelectMany(p => new[] { p.Income, p.Expenses, p.Profit }).Where(v => v >= 0);
        double maxVal = allVals.Any() ? (double)allVals.Max() : 1;
        if (maxVal == 0) maxVal = 1;

        double xStep = chartW / Math.Max(pts.Count - 1, 1);

        // Draw grid lines + Y labels
        for (int g = 0; g <= 4; g++)
        {
            double y = padT + chartH * g / 4;
            var line = new Avalonia.Controls.Shapes.Line
            {
                StartPoint = new Point(padL, y),
                EndPoint = new Point(padL + chartW, y),
                Stroke = Brushes.LightGray,
                StrokeThickness = 1
            };
            canvas.Children.Add(line);
            var label = new TextBlock
            {
                Text = $"${maxVal * (4 - g) / 4:F0}",
                FontSize = 9,
                Foreground = Brushes.Gray
            };
            Canvas.SetLeft(label, 2);
            Canvas.SetTop(label, y - 7);
            canvas.Children.Add(label);
        }

        // Draw X labels
        for (int i = 0; i < pts.Count; i++)
        {
            double x = padL + i * xStep;
            var label = new TextBlock { Text = pts[i].Month, FontSize = 10, Foreground = Brushes.Gray };
            Canvas.SetLeft(label, x - 12);
            Canvas.SetTop(label, h - padB + 6);
            canvas.Children.Add(label);
        }

        // Draw lines
        DrawLine(canvas, pts, p => (double)p.Income, maxVal, xStep, padL, padT, chartH, Color.Parse("#3B82F6"));
        DrawLine(canvas, pts, p => (double)p.Expenses, maxVal, xStep, padL, padT, chartH, Color.Parse("#EF4444"));
        DrawLine(canvas, pts, p => (double)p.Profit, maxVal, xStep, padL, padT, chartH, Color.Parse("#10B981"));

        // Legend — centered at bottom
        double legendW = 220;
        double legendX = (w - legendW) / 2;
        double legendY = h - 14;
        AddLegend(canvas, legendX, legendY, "#3B82F6", "Income");
        AddLegend(canvas, legendX + 80, legendY, "#EF4444", "Expenses");
        AddLegend(canvas, legendX + 165, legendY, "#10B981", "Profit");
    }

    private static void DrawLine(Canvas canvas, List<ChartPoint> pts,
        Func<ChartPoint, double> getValue, double maxVal,
        double xStep, double padL, double padT, double chartH, Color color)
    {
        var brush = new SolidColorBrush(color);
        for (int i = 0; i < pts.Count - 1; i++)
        {
            double x1 = padL + i * xStep;
            double y1 = padT + chartH * (1 - getValue(pts[i]) / maxVal);
            double x2 = padL + (i + 1) * xStep;
            double y2 = padT + chartH * (1 - getValue(pts[i + 1]) / maxVal);
            var line = new Avalonia.Controls.Shapes.Line
            {
                StartPoint = new Point(x1, y1),
                EndPoint = new Point(x2, y2),
                Stroke = brush,
                StrokeThickness = 2.5
            };
            canvas.Children.Add(line);
        }
        // Dots
        foreach (var (pt, i) in pts.Select((p, i) => (p, i)))
        {
            double cx = padL + i * xStep;
            double cy = padT + chartH * (1 - getValue(pt) / maxVal);
            var dot = new Avalonia.Controls.Shapes.Ellipse
            {
                Width = 7, Height = 7,
                Fill = brush
            };
            Canvas.SetLeft(dot, cx - 3.5);
            Canvas.SetTop(dot, cy - 3.5);
            canvas.Children.Add(dot);
        }
    }

    private static void AddLegend(Canvas canvas, double x, double y, string colorHex, string label)
    {
        var dot = new Avalonia.Controls.Shapes.Ellipse
        {
            Width = 8, Height = 8,
            Fill = new SolidColorBrush(Color.Parse(colorHex))
        };
        Canvas.SetLeft(dot, x);
        Canvas.SetTop(dot, y - 4);
        canvas.Children.Add(dot);
        var tb = new TextBlock { Text = label, FontSize = 10, Foreground = Brushes.Gray };
        Canvas.SetLeft(tb, x + 11);
        Canvas.SetTop(tb, y - 7);
        canvas.Children.Add(tb);
    }

    private void ExportPL()
    {
        var content = $"""
            PROFIT & LOSS STATEMENT
            Period: {_vm.FromDate:yyyy-MM-dd} to {_vm.ToDate:yyyy-MM-dd}
            ─────────────────────────────────────
            Total Revenue:          ${_vm.TotalRevenue:F2}
            Cost of Goods Sold:     ${_vm.TotalPurchases:F2}
            ─────────────────────────────────────
            Gross Profit:           ${_vm.GrossProfit:F2}
            Total Expenses:         ${_vm.TotalExpenses:F2}
            ─────────────────────────────────────
            NET PROFIT:             ${_vm.NetProfit:F2}

            Cash Balance:           ${_vm.CashBalance:F2}
            Customer Credit (Due):  ${_vm.CustomerCredit:F2}
            Supplier Payables:      ${_vm.SupplierPayables:F2}
            Stock Value:            ${_vm.StockValue:F2}
            """;
        var folder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var path = System.IO.Path.Combine(folder, $"PL-{DateTime.Now:yyyyMMdd-HHmm}.txt");
        System.IO.File.WriteAllText(path, content);
    }
}
