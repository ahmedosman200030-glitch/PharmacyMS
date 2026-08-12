using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using PharmacyMS.Desktop.ViewModels;

namespace PharmacyMS.Desktop.Views.Dashboard;

public partial class DashboardView : UserControl
{
    private readonly DashboardViewModel _vm;

    private static readonly IBrush TrendUpBrush = new SolidColorBrush(Color.Parse("#16A34A"));
    private static readonly IBrush TrendDownBrush = new SolidColorBrush(Color.Parse("#EF4444"));
    private static readonly IBrush LineBrush = new SolidColorBrush(Color.Parse("#EF4444"));
    private static readonly IBrush AreaBrush = new SolidColorBrush(Color.Parse("#FEE2E2")) { Opacity = 0.6 };
    private static readonly IBrush GridLineBrush = new SolidColorBrush(Color.Parse("#F1F1F1"));
    private static readonly IBrush TooltipBackground = new SolidColorBrush(Color.Parse("#111827"));
    private static readonly IBrush GuideLineBrush = new SolidColorBrush(Color.Parse("#D1D5DB"));

    private List<Point> _chartPoints = new();
    private List<double> _chartValues = new();
    private List<string> _chartLabels = new();

    private Ellipse? _hoverDot;
    private Avalonia.Controls.Shapes.Line? _hoverGuideLine;
    private Border? _tooltip;
    private TextBlock? _tooltipText;

    public DashboardView() { InitializeComponent(); }

    public DashboardView(DashboardViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        LowStockList.ItemsSource = _vm.LowStockRows;
        PaymentLegendList.ItemsSource = _vm.PaymentBreakdown;
        TopMedicinesList.ItemsSource = _vm.TopMedicines;
        RecentTransactionsList.ItemsSource = _vm.RecentTransactions;

        SalesChartCanvas.SizeChanged += (_, _) => DrawSalesChart();
        SalesChartCanvas.PointerMoved += SalesChartCanvas_PointerMoved;
        SalesChartCanvas.PointerExited += SalesChartCanvas_PointerExited;

        ChartRangeCombo.SelectionChanged += async (_, _) => await OnChartRangeChanged();

        AttachedToVisualTree += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        await _vm.LoadAsync();

        WelcomeText.Text = "Welcome back, Admin! Here's what's happening today.";
        TodayDateText.Text = "Today, " + DateTime.Now.ToString("d MMM yyyy");

        RevenueText.Text = $"${_vm.TodayRevenue:N2}";
        ProfitText.Text = $"${_vm.TodayProfit:N2}";
        TransactionsText.Text = _vm.TodayTransactions.ToString();
        NewCustomersText.Text = _vm.NewCustomersToday.ToString();

        SetTrend(RevenueTrendText, _vm.RevenueTrendText, _vm.RevenueTrendUp);
        SetTrend(ProfitTrendText, _vm.ProfitTrendText, _vm.ProfitTrendUp);
        SetTrend(TransactionsTrendText, _vm.TransactionsTrendText, _vm.TransactionsTrendUp);
        SetTrend(NewCustomersTrendText, _vm.NewCustomersTrendText, _vm.NewCustomersTrendUp);

        DrawSalesChart();
        DrawPaymentDonut();

        decimal totalPayments = 0;
        foreach (var p in _vm.PaymentBreakdown) totalPayments += p.Amount;
        PaymentTotalText.Text = $"${totalPayments:N2}";
    }

    private async Task OnChartRangeChanged()
    {
        var range = ChartRangeCombo.SelectedIndex switch
        {
            0 => ChartRange.Last7Days,
            1 => ChartRange.Last4Weeks,
            2 => ChartRange.Last6Months,
            _ => ChartRange.Last7Days
        };
        await _vm.LoadSalesSeriesAsync(range);
        DrawSalesChart();
    }

    private static void SetTrend(TextBlock block, string text, bool up)
    {
        block.Text = (up ? "\u2191 " : "\u2193 ") + text;
        block.Foreground = up ? TrendUpBrush : TrendDownBrush;
    }

    /// <summary>
    /// Draws the sales line + filled area on SalesChartCanvas based on _vm.SalesSeries,
    /// plus the Y-axis value labels and X-axis category labels. Re-run on SizeChanged
    /// and whenever the selected chart range changes.
    /// </summary>
    private void DrawSalesChart()
    {
        if (_vm == null) return;
        var canvas = SalesChartCanvas;
        canvas.Children.Clear();
        YAxisCanvas.Children.Clear();
        XAxisCanvas.Children.Clear();
        _chartPoints = new List<Point>();
        _chartValues = new List<double>();
        _chartLabels = new List<string>();
        HideTooltip();

        double width = canvas.Bounds.Width;
        double height = canvas.Bounds.Height;
        if (width <= 0 || height <= 0 || _vm.SalesSeries.Count == 0) return;

        foreach (var p in _vm.SalesSeries)
        {
            _chartValues.Add((double)p.Amount);
            _chartLabels.Add(p.Label);
        }

        double maxValue = 1;
        foreach (var v in _chartValues) if (v > maxValue) maxValue = v;
        maxValue *= 1.15; // headroom so the peak isn't glued to the top

        const int gridLines = 4;
        for (int i = 0; i <= gridLines; i++)
        {
            double y = height / gridLines * i;
            var line = new Avalonia.Controls.Shapes.Line
            {
                StartPoint = new Point(0, y),
                EndPoint = new Point(width, y),
                Stroke = GridLineBrush,
                StrokeThickness = 1,
                StrokeDashArray = new AvaloniaList<double> { 3, 3 }
            };
            canvas.Children.Add(line);

            double axisValue = maxValue * (1 - (double)i / gridLines);
            var label = new TextBlock
            {
                Text = FormatAxisValue(axisValue),
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.Parse("#9CA3AF")),
                Width = 40,
                TextAlignment = TextAlignment.Right
            };
            Canvas.SetLeft(label, 0);
            Canvas.SetTop(label, y - 7);
            YAxisCanvas.Children.Add(label);
        }

        int count = _chartValues.Count;
        double stepX = count > 1 ? width / (count - 1) : 0;
        const double topPad = 10, bottomPad = 10;
        double plotHeight = height - topPad - bottomPad;

        var areaPoints = new List<Point>();
        areaPoints.Add(new Point(0, height));

        for (int i = 0; i < count; i++)
        {
            double x = stepX * i;
            double y = topPad + plotHeight - (_chartValues[i] / maxValue * plotHeight);
            _chartPoints.Add(new Point(x, y));
            areaPoints.Add(new Point(x, y));
        }
        areaPoints.Add(new Point(width, height));

        var area = new Polygon { Points = areaPoints, Fill = AreaBrush };
        canvas.Children.Add(area);

        var line2 = new Polyline { Points = _chartPoints, Stroke = LineBrush, StrokeThickness = 2.5 };
        canvas.Children.Add(line2);

        foreach (var pt in _chartPoints)
        {
            var dot = new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = Brushes.White,
                Stroke = LineBrush,
                StrokeThickness = 2
            };
            Canvas.SetLeft(dot, pt.X - 4);
            Canvas.SetTop(dot, pt.Y - 4);
            canvas.Children.Add(dot);
        }

        // X-axis category labels, one centered under each point
        double xAxisWidth = XAxisCanvas.Bounds.Width > 0 ? XAxisCanvas.Bounds.Width : width;
        for (int i = 0; i < count; i++)
        {
            var lbl = new TextBlock
            {
                Text = _chartLabels[i],
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.Parse("#9CA3AF")),
                Width = 60,
                TextAlignment = TextAlignment.Center
            };
            double cx = count > 1 ? (stepX * i) : (xAxisWidth / 2);
            Canvas.SetLeft(lbl, cx - 30);
            Canvas.SetTop(lbl, 0);
            XAxisCanvas.Children.Add(lbl);
        }

        // Hover elements (created once per draw, hidden until pointer moves over a point)
        _hoverGuideLine = new Avalonia.Controls.Shapes.Line
        {
            Stroke = GuideLineBrush,
            StrokeThickness = 1,
            StrokeDashArray = new AvaloniaList<double> { 4, 3 },
            IsVisible = false
        };
        canvas.Children.Add(_hoverGuideLine);

        _hoverDot = new Ellipse
        {
            Width = 12,
            Height = 12,
            Fill = LineBrush,
            Stroke = Brushes.White,
            StrokeThickness = 2,
            IsVisible = false
        };
        canvas.Children.Add(_hoverDot);

        _tooltipText = new TextBlock
        {
            Foreground = Brushes.White,
            FontSize = 12,
            FontWeight = FontWeight.Bold
        };
        _tooltip = new Border
        {
            Background = TooltipBackground,
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10, 6),
            IsVisible = false,
            Child = _tooltipText
        };
        canvas.Children.Add(_tooltip);
    }

    private void SalesChartCanvas_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_chartPoints.Count == 0 || _hoverDot == null || _hoverGuideLine == null || _tooltip == null || _tooltipText == null)
            return;

        var pos = e.GetPosition(SalesChartCanvas);

        int nearestIndex = 0;
        double nearestDist = double.MaxValue;
        for (int i = 0; i < _chartPoints.Count; i++)
        {
            double dist = Math.Abs(_chartPoints[i].X - pos.X);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearestIndex = i;
            }
        }

        var point = _chartPoints[nearestIndex];
        double value = _chartValues[nearestIndex];
        string label = _chartLabels[nearestIndex];

        _hoverDot.IsVisible = true;
        Canvas.SetLeft(_hoverDot, point.X - 6);
        Canvas.SetTop(_hoverDot, point.Y - 6);

        _hoverGuideLine.IsVisible = true;
        _hoverGuideLine.StartPoint = new Point(point.X, 0);
        _hoverGuideLine.EndPoint = new Point(point.X, SalesChartCanvas.Bounds.Height);

        _tooltipText.Text = $"{label}: ${value:N2}";
        _tooltip.IsVisible = true;
        _tooltip.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        double tooltipWidth = _tooltip.DesiredSize.Width;
        double tooltipLeft = point.X - tooltipWidth / 2;
        double canvasWidth = SalesChartCanvas.Bounds.Width;
        if (tooltipLeft < 0) tooltipLeft = 0;
        if (tooltipLeft + tooltipWidth > canvasWidth) tooltipLeft = canvasWidth - tooltipWidth;
        Canvas.SetLeft(_tooltip, tooltipLeft);
        Canvas.SetTop(_tooltip, point.Y - _tooltip.DesiredSize.Height - 14);
    }

    private void SalesChartCanvas_PointerExited(object? sender, PointerEventArgs e)
    {
        HideTooltip();
    }

    private void HideTooltip()
    {
        if (_hoverDot != null) _hoverDot.IsVisible = false;
        if (_hoverGuideLine != null) _hoverGuideLine.IsVisible = false;
        if (_tooltip != null) _tooltip.IsVisible = false;
    }

    private static string FormatAxisValue(double value)
    {
        if (value >= 1000)
            return $"${value / 1000:0.#}k";
        return $"${value:0}";
    }

    /// <summary>
    /// Draws a multi-segment donut ring on PaymentDonutCanvas, one arc per payment method,
    /// proportional to its share of this month's sales.
    /// </summary>
    private void DrawPaymentDonut()
    {
        var canvas = PaymentDonutCanvas;
        for (int i = canvas.Children.Count - 1; i >= 0; i--)
        {
            if (canvas.Children[i] is Avalonia.Controls.Shapes.Path)
                canvas.Children.RemoveAt(i);
        }

        if (_vm.PaymentBreakdown.Count == 0) return;

        const double cx = 90, cy = 90, r = 64, strokeThickness = 36;
        double startAngle = 0;

        foreach (var slice in _vm.PaymentBreakdown)
        {
            double sweep = slice.Percent / 100.0 * 360.0;
            if (sweep <= 0) continue;
            double endAngle = startAngle + sweep;

            var path = new Avalonia.Controls.Shapes.Path
            {
                Data = Geometry.Parse(BuildArcSegment(startAngle, Math.Min(endAngle, startAngle + 359.5), cx, cy, r)),
                Stroke = new SolidColorBrush(Color.Parse(slice.Color)),
                StrokeThickness = strokeThickness,
                StrokeLineCap = PenLineCap.Flat
            };
            canvas.Children.Insert(0, path);
            startAngle = endAngle;
        }
    }

    private static string BuildArcSegment(double startDeg, double endDeg, double cx, double cy, double r)
    {
        var startRad = startDeg * Math.PI / 180.0;
        var endRad = endDeg * Math.PI / 180.0;
        var x1 = cx + r * Math.Sin(startRad);
        var y1 = cy - r * Math.Cos(startRad);
        var x2 = cx + r * Math.Sin(endRad);
        var y2 = cy - r * Math.Cos(endRad);
        var largeArc = (endDeg - startDeg) > 180 ? 1 : 0;
        var ic = CultureInfo.InvariantCulture;
        return $"M {x1.ToString("F2", ic)},{y1.ToString("F2", ic)} A {r.ToString("F2", ic)},{r.ToString("F2", ic)} 0 {largeArc} 1 {x2.ToString("F2", ic)},{y2.ToString("F2", ic)}";
    }
}
