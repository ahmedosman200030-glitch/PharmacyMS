using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using PharmacyMS.Desktop.ViewModels;

namespace PharmacyMS.Desktop.Views.Accounting;

public partial class CashFlowView : UserControl
{
    private readonly CashFlowViewModel _vm;

    public CashFlowView() { InitializeComponent(); }

    public CashFlowView(CashFlowViewModel vm)
    {
        InitializeComponent();
        _vm = vm;

        RangePicker.SetRange(_vm.FromDate, _vm.ToDate);

        RefreshButton.Click += async (_, _) =>
        {
            _vm.FromDate = RangePicker.FromDate;
            _vm.ToDate = RangePicker.ToDate;
            await LoadAndRender();
        };

        AllFilterBtn.Click += (_, _) => { _vm.ActiveFilter = "All"; _vm.CurrentPage = 1; RenderTable(); UpdateFilterHighlight(); };
        InFilterBtn.Click += (_, _) => { _vm.ActiveFilter = "In"; _vm.CurrentPage = 1; RenderTable(); UpdateFilterHighlight(); };
        OutFilterBtn.Click += (_, _) => { _vm.ActiveFilter = "Out"; _vm.CurrentPage = 1; RenderTable(); UpdateFilterHighlight(); };

        SearchBox.TextChanged += (_, _) => { _vm.SearchText = SearchBox.Text ?? string.Empty; _vm.CurrentPage = 1; RenderTable(); };

        PrevPageBtn.Click += (_, _) => { if (_vm.CurrentPage > 1) { _vm.CurrentPage--; RenderTable(); } };
        NextPageBtn.Click += (_, _) =>
        {
            var filtered = _vm.GetFilteredRows();
            if (_vm.CurrentPage < _vm.TotalPages(filtered)) { _vm.CurrentPage++; RenderTable(); }
        };

        TrendCanvas.SizeChanged += (_, _) => DrawTrend();

        AttachedToVisualTree += async (_, _) => await LoadAndRender();
    }

    private async Task LoadAndRender()
    {
        await _vm.LoadAsync();
        RefreshStats();
        RenderTable();
        UpdateFilterHighlight();
        DrawTrend();
    }

    private void RefreshStats()
    {
        CashInText.Text = $"${_vm.TotalCashIn:F2}";
        CashOutText.Text = $"${_vm.TotalCashOut:F2}";
        NetCashFlowText.Text = $"${_vm.NetCashFlow:F2}";
        NetCashFlowText.Foreground = new SolidColorBrush(Color.Parse(_vm.NetCashFlow >= 0 ? "#10B981" : "#EF4444"));
        CashBalanceText.Text = $"${_vm.CashBalance:F2}";
    }

    private void RenderTable()
    {
        var filtered = _vm.GetFilteredRows();
        var page = _vm.GetPage(filtered);
        RowsGrid.ItemsSource = page;

        var totalPages = _vm.TotalPages(filtered);
        PageLabelText.Text = $"Page {_vm.CurrentPage} of {totalPages}";

        if (filtered.Count == 0)
            PagingSummaryText.Text = "No transactions";
        else
        {
            var start = (_vm.CurrentPage - 1) * _vm.PageSize + 1;
            var end = Math.Min(_vm.CurrentPage * _vm.PageSize, filtered.Count);
            PagingSummaryText.Text = $"Showing {start} to {end} of {filtered.Count} entries";
        }

        PrevPageBtn.IsEnabled = _vm.CurrentPage > 1;
        NextPageBtn.IsEnabled = _vm.CurrentPage < totalPages;
    }

    private void UpdateFilterHighlight()
    {
        var active = new SolidColorBrush(Color.Parse("#3B82F6"));
        var inactive = new SolidColorBrush(Color.Parse("#F1F5F9"));
        AllFilterBtn.Background = _vm.ActiveFilter == "All" ? active : inactive;
        AllFilterBtn.Foreground = _vm.ActiveFilter == "All" ? Brushes.White : Brushes.Black;
        InFilterBtn.Background = _vm.ActiveFilter == "In" ? active : inactive;
        InFilterBtn.Foreground = _vm.ActiveFilter == "In" ? Brushes.White : Brushes.Black;
        OutFilterBtn.Background = _vm.ActiveFilter == "Out" ? active : inactive;
        OutFilterBtn.Foreground = _vm.ActiveFilter == "Out" ? Brushes.White : Brushes.Black;
    }

    private void DrawTrend()
    {
        var canvas = TrendCanvas;
        canvas.Children.Clear();
        var pts = _vm.TrendPoints;
        if (pts == null || pts.Count == 0)
        {
            var empty = new TextBlock { Text = "Select a range of 62 days or fewer to see the daily trend", Foreground = Brushes.Gray, FontSize = 12 };
            Canvas.SetLeft(empty, 10); Canvas.SetTop(empty, 90);
            canvas.Children.Add(empty);
            return;
        }

        double w = canvas.Bounds.Width > 0 ? canvas.Bounds.Width : 800;
        double h = canvas.Bounds.Height > 0 ? canvas.Bounds.Height : 220;
        double padL = 55, padR = 10, padT = 10, padB = 28;
        double chartW = w - padL - padR;
        double chartH = h - padT - padB;

        double maxVal = Math.Max(
            pts.Count > 0 ? (double)pts.Max(p => p.CashIn) : 0,
            pts.Count > 0 ? (double)pts.Max(p => p.CashOut) : 0);
        if (maxVal <= 0) maxVal = 1;

        double xStep = pts.Count > 1 ? chartW / (pts.Count - 1) : 0;
        double YFor(decimal amount) => padT + chartH * (1 - (double)amount / maxVal);

        for (int g = 0; g <= 3; g++)
        {
            double y = padT + chartH * g / 3;
            double val = maxVal - maxVal * g / 3;
            canvas.Children.Add(new Line { StartPoint = new Point(padL, y), EndPoint = new Point(padL + chartW, y), Stroke = Brushes.LightGray, StrokeThickness = 1 });
            var label = new TextBlock { Text = $"${val:F0}", FontSize = 9, Foreground = Brushes.Gray };
            Canvas.SetLeft(label, 2); Canvas.SetTop(label, y - 6);
            canvas.Children.Add(label);
        }

        DrawLine(canvas, pts.Select(p => p.CashIn).ToList(), xStep, padL, YFor, Color.Parse("#10B981"));
        DrawLine(canvas, pts.Select(p => p.CashOut).ToList(), xStep, padL, YFor, Color.Parse("#EF4444"));

        int[] showIdx = pts.Count <= 5 ? Enumerable.Range(0, pts.Count).ToArray()
            : new[] { 0, pts.Count / 4, pts.Count / 2, 3 * pts.Count / 4, pts.Count - 1 };
        foreach (var i in showIdx.Distinct())
        {
            double x = padL + i * xStep;
            var label = new TextBlock { Text = pts[i].Label, FontSize = 9, Foreground = Brushes.Gray };
            Canvas.SetLeft(label, x - 16); Canvas.SetTop(label, h - padB + 6);
            canvas.Children.Add(label);
        }
    }

    private static void DrawLine(Canvas canvas, List<decimal> values, double xStep, double padL, Func<decimal, double> yFor, Color color)
    {
        var brush = new SolidColorBrush(color);
        for (int i = 0; i < values.Count - 1; i++)
        {
            double x1 = padL + i * xStep, y1 = yFor(values[i]);
            double x2 = padL + (i + 1) * xStep, y2 = yFor(values[i + 1]);
            canvas.Children.Add(new Line { StartPoint = new Point(x1, y1), EndPoint = new Point(x2, y2), Stroke = brush, StrokeThickness = 2 });
        }
        for (int i = 0; i < values.Count; i++)
        {
            double x = padL + i * xStep, y = yFor(values[i]);
            var dot = new Ellipse { Width = 5, Height = 5, Fill = brush };
            Canvas.SetLeft(dot, x - 2.5); Canvas.SetTop(dot, y - 2.5);
            canvas.Children.Add(dot);
        }
    }
}
