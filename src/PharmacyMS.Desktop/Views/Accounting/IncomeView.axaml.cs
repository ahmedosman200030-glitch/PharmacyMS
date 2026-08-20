using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using PharmacyMS.Desktop.ViewModels;

namespace PharmacyMS.Desktop.Views.Accounting;

public partial class IncomeView : UserControl
{
    private readonly IncomeViewModel _vm;

    public IncomeView() { InitializeComponent(); }

    public IncomeView(IncomeViewModel vm)
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
        CashFilterBtn.Click += (_, _) => { _vm.ActiveFilter = "Cash Sales"; _vm.CurrentPage = 1; RenderTable(); UpdateFilterHighlight(); };
        PaymentFilterBtn.Click += (_, _) => { _vm.ActiveFilter = "Customer Payment"; _vm.CurrentPage = 1; RenderTable(); UpdateFilterHighlight(); };

        SearchBox.TextChanged += (_, _) => { _vm.SearchText = SearchBox.Text ?? string.Empty; _vm.CurrentPage = 1; RenderTable(); };

        PrevPageBtn.Click += (_, _) => { if (_vm.CurrentPage > 1) { _vm.CurrentPage--; RenderTable(); } };
        NextPageBtn.Click += (_, _) =>
        {
            var filtered = _vm.GetFilteredRows();
            if (_vm.CurrentPage < _vm.TotalPages(filtered)) { _vm.CurrentPage++; RenderTable(); }
        };

        DonutCanvas.SizeChanged += (_, _) => DrawDonut();
        TrendCanvas.SizeChanged += (_, _) => DrawTrend();

        AttachedToVisualTree += async (_, _) => await LoadAndRender();
    }

    private async Task LoadAndRender()
    {
        await _vm.LoadAsync();
        RefreshStats();
        RenderTable();
        UpdateFilterHighlight();
        DrawDonut();
        DrawTrend();
    }

    private void RefreshStats()
    {
        TotalIncomeText.Text = $"${_vm.TotalIncome:F2}";
        CashSalesText.Text = $"${_vm.CashSalesTotal:F2}";
        CustomerPaymentsText.Text = $"${_vm.CustomerPaymentsTotal:F2}";
        TxnCountText.Text = _vm.TransactionCount.ToString();

        var period = $"{_vm.FromDate:MMM d} - {_vm.ToDate:MMM d}";
        TotalIncomePeriodText.Text = period;
        CashSalesPeriodText.Text = period;
        CustomerPaymentsPeriodText.Text = period;
        TxnCountPeriodText.Text = period;

        var total = _vm.TotalIncome == 0 ? 1 : _vm.TotalIncome;
        TopCashText.Text = $"${_vm.CashSalesTotal:F2}";
        TopCashPctText.Text = $"{_vm.CashSalesTotal / total:P1}";
        TopPaymentsText.Text = $"${_vm.CustomerPaymentsTotal:F2}";
        TopPaymentsPctText.Text = $"{_vm.CustomerPaymentsTotal / total:P1}";
        TopTotalText.Text = $"${_vm.TotalIncome:F2}";
    }

    private void RenderTable()
    {
        var filtered = _vm.GetFilteredRows();
        var page = _vm.GetPage(filtered);
        IncomeGrid.ItemsSource = page;

        var totalPages = _vm.TotalPages(filtered);
        PageLabelText.Text = $"Page {_vm.CurrentPage} of {totalPages}";

        if (filtered.Count == 0)
        {
            PagingSummaryText.Text = "No entries";
        }
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
        var active = new SolidColorBrush(Color.Parse("#2563EB"));
        var inactive = new SolidColorBrush(Color.Parse("#F1F5F9"));
        AllFilterBtn.Background = _vm.ActiveFilter == "All" ? active : inactive;
        AllFilterBtn.Foreground = _vm.ActiveFilter == "All" ? Brushes.White : Brushes.Black;
        CashFilterBtn.Background = _vm.ActiveFilter == "Cash Sales" ? active : inactive;
        CashFilterBtn.Foreground = _vm.ActiveFilter == "Cash Sales" ? Brushes.White : Brushes.Black;
        PaymentFilterBtn.Background = _vm.ActiveFilter == "Customer Payment" ? active : inactive;
        PaymentFilterBtn.Foreground = _vm.ActiveFilter == "Customer Payment" ? Brushes.White : Brushes.Black;
    }

    private void DrawDonut()
    {
        var canvas = DonutCanvas;
        canvas.Children.Clear();
        double w = canvas.Bounds.Width > 0 ? canvas.Bounds.Width : 300;
        double h = canvas.Bounds.Height > 0 ? canvas.Bounds.Height : 160;
        double cx = w / 2 - 60, cy = h / 2, radius = Math.Min(h, w / 2) / 2 - 8, thickness = 18;

        var total = _vm.CashSalesTotal + _vm.CustomerPaymentsTotal;
        if (total <= 0)
        {
            var empty = new TextBlock { Text = "No income yet", Foreground = Brushes.Gray, FontSize = 12 };
            Canvas.SetLeft(empty, 10); Canvas.SetTop(empty, h / 2 - 8);
            canvas.Children.Add(empty);
            return;
        }

        double cashFrac = (double)(_vm.CashSalesTotal / total);
        DrawArcSegment(canvas, cx, cy, radius, thickness, 0, cashFrac * 360, Color.Parse("#10B981"));
        DrawArcSegment(canvas, cx, cy, radius, thickness, cashFrac * 360, 360, Color.Parse("#F59E0B"));

        double lx = w - 110, ly = h / 2 - 24;
        AddDonutLegendItem(canvas, lx, ly, "#10B981", "Cash Sales", $"{cashFrac:P0}");
        AddDonutLegendItem(canvas, lx, ly + 24, "#F59E0B", "Customer Pay.", $"{(1 - cashFrac):P0}");
    }

    private static void AddDonutLegendItem(Canvas canvas, double x, double y, string colorHex, string label, string pct)
    {
        var dot = new Ellipse { Width = 8, Height = 8, Fill = new SolidColorBrush(Color.Parse(colorHex)) };
        Canvas.SetLeft(dot, x); Canvas.SetTop(dot, y);
        canvas.Children.Add(dot);
        var tb = new TextBlock { Text = $"{label} ({pct})", FontSize = 10, Foreground = Brushes.Gray };
        Canvas.SetLeft(tb, x + 12); Canvas.SetTop(tb, y - 3);
        canvas.Children.Add(tb);
    }

    private static void DrawArcSegment(Canvas canvas, double cx, double cy, double radius, double thickness,
        double startDeg, double endDeg, Color color)
    {
        if (endDeg - startDeg <= 0.01) return;
        double startRad = (Math.PI / 180) * (startDeg - 90);
        double endRad = (Math.PI / 180) * (endDeg - 90);

        var outerStart = new Point(cx + radius * Math.Cos(startRad), cy + radius * Math.Sin(startRad));
        var outerEnd = new Point(cx + radius * Math.Cos(endRad), cy + radius * Math.Sin(endRad));
        var innerRadius = radius - thickness;
        var innerEnd = new Point(cx + innerRadius * Math.Cos(endRad), cy + innerRadius * Math.Sin(endRad));
        var innerStart = new Point(cx + innerRadius * Math.Cos(startRad), cy + innerRadius * Math.Sin(startRad));

        bool isLargeArc = (endDeg - startDeg) > 180;

        var figure = new PathFigure { StartPoint = outerStart, IsClosed = true };
        figure.Segments.Add(new ArcSegment
        {
            Point = outerEnd, Size = new Size(radius, radius),
            IsLargeArc = isLargeArc, SweepDirection = SweepDirection.Clockwise
        });
        figure.Segments.Add(new LineSegment { Point = innerEnd });
        figure.Segments.Add(new ArcSegment
        {
            Point = innerStart, Size = new Size(innerRadius, innerRadius),
            IsLargeArc = isLargeArc, SweepDirection = SweepDirection.CounterClockwise
        });

        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);

        var path = new Avalonia.Controls.Shapes.Path
        {
            Data = geometry,
            Fill = new SolidColorBrush(color)
        };
        canvas.Children.Add(path);
    }

    private void DrawTrend()
    {
        var canvas = TrendCanvas;
        canvas.Children.Clear();
        var pts = _vm.TrendPoints;
        if (pts == null || pts.Count == 0) return;

        double w = canvas.Bounds.Width > 0 ? canvas.Bounds.Width : 300;
        double h = canvas.Bounds.Height > 0 ? canvas.Bounds.Height : 160;
        double padL = 45, padR = 10, padT = 10, padB = 24;
        double chartW = w - padL - padR;
        double chartH = h - padT - padB;

        double maxVal = pts.Count > 0 ? (double)pts.Max(p => p.Amount) : 1;
        if (maxVal <= 0) maxVal = 1;

        double xStep = pts.Count > 1 ? chartW / (pts.Count - 1) : 0;

        for (int g = 0; g <= 2; g++)
        {
            double y = padT + chartH * g / 2;
            canvas.Children.Add(new Line
            {
                StartPoint = new Point(padL, y), EndPoint = new Point(padL + chartW, y),
                Stroke = Brushes.LightGray, StrokeThickness = 1
            });
            var label = new TextBlock { Text = $"${maxVal * (2 - g) / 2:F0}", FontSize = 8, Foreground = Brushes.Gray };
            Canvas.SetLeft(label, 2); Canvas.SetTop(label, y - 6);
            canvas.Children.Add(label);
        }

        var brush = new SolidColorBrush(Color.Parse("#3B82F6"));
        for (int i = 0; i < pts.Count - 1; i++)
        {
            double x1 = padL + i * xStep;
            double y1 = padT + chartH * (1 - (double)pts[i].Amount / maxVal);
            double x2 = padL + (i + 1) * xStep;
            double y2 = padT + chartH * (1 - (double)pts[i + 1].Amount / maxVal);
            canvas.Children.Add(new Line { StartPoint = new Point(x1, y1), EndPoint = new Point(x2, y2), Stroke = brush, StrokeThickness = 2 });
        }
        foreach (var (pt, i) in pts.Select((p, i) => (p, i)))
        {
            double cx = padL + i * xStep;
            double cy = padT + chartH * (1 - (double)pt.Amount / maxVal);
            var dot = new Ellipse { Width = 5, Height = 5, Fill = brush };
            Canvas.SetLeft(dot, cx - 2.5); Canvas.SetTop(dot, cy - 2.5);
            canvas.Children.Add(dot);

            if (pt.Amount > 0)
            {
                var amtLabel = new TextBlock
                {
                    Text = $"${pt.Amount:F0}",
                    FontSize = 8,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = brush
                };
                Canvas.SetLeft(amtLabel, cx - 12);
                Canvas.SetTop(amtLabel, cy - 16);
                canvas.Children.Add(amtLabel);
            }
        }

        int[] showIdx = pts.Count <= 3 ? Enumerable.Range(0, pts.Count).ToArray()
            : new[] { 0, pts.Count / 2, pts.Count - 1 };
        foreach (var i in showIdx)
        {
            double x = padL + i * xStep;
            var label = new TextBlock { Text = pts[i].Label, FontSize = 8, Foreground = Brushes.Gray };
            Canvas.SetLeft(label, x - 14); Canvas.SetTop(label, h - padB + 4);
            canvas.Children.Add(label);
        }
    }
}
