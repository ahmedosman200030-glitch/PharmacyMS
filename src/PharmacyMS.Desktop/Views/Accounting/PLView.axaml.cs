using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using PharmacyMS.Desktop.ViewModels;

namespace PharmacyMS.Desktop.Views.Accounting;

public partial class PLView : UserControl
{
    private readonly PLViewModel _vm;

    public PLView() { InitializeComponent(); }

    public PLView(PLViewModel vm)
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

        DonutCanvas.SizeChanged += (_, _) => DrawDonut();
        TrendCanvas.SizeChanged += (_, _) => DrawTrend();

        AttachedToVisualTree += async (_, _) => await LoadAndRender();
    }

    private async Task LoadAndRender()
    {
        await _vm.LoadAsync();
        RefreshStatCards();
        RenderStatement();
        RenderRatios();
        DrawDonut();
        DrawTrend();
    }

    private void RefreshStatCards()
    {
        RevenueText.Text = $"${_vm.TotalRevenue:F2}";
        RevenueChangeText.Text = FormatChange(_vm.RevenueChangePercent, _vm.PriorPeriodLabel);
        CogsText.Text = $"${_vm.CostOfGoodsSold:F2}";
        CogsChangeText.Text = FormatChange(_vm.CogsChangePercent, _vm.PriorPeriodLabel, invertColor: true);
        GrossProfitText.Text = $"${_vm.GrossProfit:F2}";
        GrossProfitChangeText.Text = FormatChange(_vm.GrossProfitChangePercent, _vm.PriorPeriodLabel);
        ExpensesText.Text = $"${_vm.TotalExpensesAmount:F2}";
        ExpensesChangeText.Text = FormatChange(_vm.ExpensesChangePercent, _vm.PriorPeriodLabel, invertColor: true);
        NetProfitText.Text = $"${_vm.NetProfit:F2}";
        NetProfitChangeText.Text = FormatChange(_vm.NetProfitChangePercent, _vm.PriorPeriodLabel);
    }

    private static string FormatChange(decimal pct, string periodLabel, bool invertColor = false)
    {
        var sign = pct >= 0 ? "+" : "";
        return $"{sign}{pct:P1} vs {periodLabel}";
    }

    private TextBlock AssignChangeColor(TextBlock tb, decimal pct, bool invertColor)
    {
        var isGood = invertColor ? pct <= 0 : pct >= 0;
        tb.Foreground = new SolidColorBrush(Color.Parse(isGood ? "#10B981" : "#EF4444"));
        return tb;
    }

    private void RenderRatios()
    {
        GpMarginText.Text = $"{_vm.GrossProfitMargin:P2}";
        NpMarginText.Text = $"{_vm.NetProfitMargin:P2}";
        ExpRatioText.Text = $"{_vm.ExpenseToRevenueRatio:P2}";
        CostRatioText.Text = $"{_vm.CostToRevenueRatio:P2}";
    }

    private void RenderStatement()
    {
        StatementPanel.Children.Clear();
        var rev = _vm.TotalRevenue > 0 ? _vm.TotalRevenue : 1;

        void AddSectionHeader(string text, string colorHex)
        {
            StatementPanel.Children.Add(new TextBlock
            {
                Text = text, FontSize = 12, FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(Color.Parse(colorHex)), Margin = new Thickness(0, 8, 0, 2)
            });
        }

        void AddRow(string label, decimal amount, bool bold = false, string? bgHex = null, bool isNegative = false)
        {
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto") };
            if (bgHex != null)
            {
                var bg = new Border { Background = new SolidColorBrush(Color.Parse(bgHex)), CornerRadius = new CornerRadius(4) };
                row.Children.Add(bg);
            }
            var fg = isNegative ? "#EF4444" : "#0F172A";
            var labelTb = new TextBlock
            {
                Text = label, FontSize = 12, Foreground = new SolidColorBrush(Color.Parse(fg)),
                FontWeight = bold ? FontWeight.Bold : FontWeight.Normal, Margin = new Thickness(4, 3, 0, 3)
            };
            var amountText = isNegative ? $"(${Math.Abs(amount):F2})" : $"${amount:F2}";
            var amountTb = new TextBlock
            {
                Text = amountText, FontSize = 12, Width = 110, HorizontalAlignment = HorizontalAlignment.Right,
                Foreground = new SolidColorBrush(Color.Parse(fg)), FontWeight = bold ? FontWeight.Bold : FontWeight.Normal,
                Margin = new Thickness(0, 3, 0, 3)
            };
            var pct = amount / rev;
            var pctTb = new TextBlock
            {
                Text = $"{pct:P2}", FontSize = 12, Width = 110, HorizontalAlignment = HorizontalAlignment.Right,
                Foreground = new SolidColorBrush(Color.Parse(fg)), FontWeight = bold ? FontWeight.Bold : FontWeight.Normal,
                Margin = new Thickness(0, 3, 4, 3)
            };
            Grid.SetColumn(labelTb, 0);
            Grid.SetColumn(amountTb, 1);
            Grid.SetColumn(pctTb, 2);
            row.Children.Add(labelTb);
            row.Children.Add(amountTb);
            row.Children.Add(pctTb);
            StatementPanel.Children.Add(row);
        }

        AddSectionHeader("INCOME", "#10B981");
        AddRow("Total Revenue", _vm.TotalRevenue);
        AddRow("Less: Sales Returns & Discounts", _vm.SalesReturnsDiscounts, isNegative: true);
        AddRow("Net Revenue", _vm.NetRevenue, bold: true);

        AddSectionHeader("COST OF GOODS SOLD", "#EF4444");
        AddRow("Opening Stock", _vm.OpeningStockValue);
        AddRow("Purchases", _vm.PurchasesValue);
        AddRow("Less: Closing Stock", _vm.ClosingStockValue, isNegative: true);
        AddRow("Total COGS", _vm.CostOfGoodsSold, bold: true);
        AddRow("GROSS PROFIT", _vm.GrossProfit, bold: true, bgHex: "#ECFDF5");

        AddSectionHeader("EXPENSES", "#EF4444");
        foreach (var line in _vm.ExpenseLines)
            AddRow($"{line.Category} Expense", line.Amount);
        AddRow("Total Expenses", _vm.TotalExpensesAmount, bold: true, bgHex: "#FEF2F2");

        AddRow("NET PROFIT", _vm.NetProfit, bold: true, bgHex: "#ECFDF5");
    }

    private void DrawDonut()
    {
        var canvas = DonutCanvas;
        canvas.Children.Clear();
        DonutLegendPanel.Children.Clear();

        double w = canvas.Bounds.Width > 0 ? canvas.Bounds.Width : 160;
        double h = canvas.Bounds.Height > 0 ? canvas.Bounds.Height : 160;
        double cx = w / 2, cy = h / 2, radius = Math.Min(h, w) / 2 - 8, thickness = 22;

        var segments = _vm.DonutSegments.Where(s => s.Amount > 0).ToList();
        var total = segments.Sum(s => s.Amount);
        if (total <= 0)
        {
            var empty = new TextBlock { Text = "No data", Foreground = Brushes.Gray, FontSize = 12 };
            Canvas.SetLeft(empty, w / 2 - 20); Canvas.SetTop(empty, h / 2 - 8);
            canvas.Children.Add(empty);
            return;
        }

        double cursor = 0;
        foreach (var seg in segments)
        {
            double frac = (double)(seg.Amount / total);
            DrawArcSegment(canvas, cx, cy, radius, thickness, cursor * 360, (cursor + frac) * 360, Color.Parse(seg.ColorHex));
            cursor += frac;
        }

        foreach (var seg in _vm.DonutSegments)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            var dot = new Ellipse { Width = 8, Height = 8, Fill = new SolidColorBrush(Color.Parse(seg.ColorHex)), VerticalAlignment = VerticalAlignment.Center };
            var label = new TextBlock { Text = $"{seg.Label}", FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#334155")), VerticalAlignment = VerticalAlignment.Center };
            var amount = new TextBlock { Text = $"${seg.Amount:F2} ({seg.Percent:P1})", FontSize = 11, Foreground = Brushes.Gray, VerticalAlignment = VerticalAlignment.Center };
            row.Children.Add(dot);
            row.Children.Add(label);
            row.Children.Add(amount);
            DonutLegendPanel.Children.Add(row);
        }
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
        figure.Segments.Add(new ArcSegment { Point = outerEnd, Size = new Size(radius, radius), IsLargeArc = isLargeArc, SweepDirection = SweepDirection.Clockwise });
        figure.Segments.Add(new LineSegment { Point = innerEnd });
        figure.Segments.Add(new ArcSegment { Point = innerStart, Size = new Size(innerRadius, innerRadius), IsLargeArc = isLargeArc, SweepDirection = SweepDirection.CounterClockwise });

        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);

        canvas.Children.Add(new Avalonia.Controls.Shapes.Path { Data = geometry, Fill = new SolidColorBrush(color) });
    }

    private void DrawTrend()
    {
        var canvas = TrendCanvas;
        canvas.Children.Clear();
        var pts = _vm.TrendPoints;
        if (pts == null || pts.Count == 0) return;

        double w = canvas.Bounds.Width > 0 ? canvas.Bounds.Width : 300;
        double h = canvas.Bounds.Height > 0 ? canvas.Bounds.Height : 150;
        double padL = 45, padR = 10, padT = 10, padB = 24;
        double chartW = w - padL - padR;
        double chartH = h - padT - padB;

        double maxVal = pts.Count > 0 ? (double)pts.Max(p => p.Amount) : 1;
        double minVal = pts.Count > 0 ? (double)pts.Min(p => p.Amount) : 0;
        if (minVal > 0) minVal = 0;
        if (maxVal <= minVal) maxVal = minVal + 1;

        double xStep = pts.Count > 1 ? chartW / (pts.Count - 1) : 0;
        double YFor(decimal amount) => padT + chartH * (1 - ((double)amount - minVal) / (maxVal - minVal));

        for (int g = 0; g <= 2; g++)
        {
            double y = padT + chartH * g / 2;
            double val = maxVal - (maxVal - minVal) * g / 2;
            canvas.Children.Add(new Line { StartPoint = new Point(padL, y), EndPoint = new Point(padL + chartW, y), Stroke = Brushes.LightGray, StrokeThickness = 1 });
            var label = new TextBlock { Text = $"${val:F0}", FontSize = 8, Foreground = Brushes.Gray };
            Canvas.SetLeft(label, 2); Canvas.SetTop(label, y - 6);
            canvas.Children.Add(label);
        }

        var strokeBrush = new SolidColorBrush(Color.Parse("#10B981"));
        var fillGeometry = new PathGeometry();
        var fillFigure = new PathFigure { StartPoint = new Point(padL, padT + chartH), IsClosed = true };
        for (int i = 0; i < pts.Count; i++)
        {
            double x = padL + i * xStep;
            double y = YFor(pts[i].Amount);
            fillFigure.Segments.Add(new LineSegment { Point = new Point(x, y) });
        }
        fillFigure.Segments.Add(new LineSegment { Point = new Point(padL + chartW, padT + chartH) });
        fillGeometry.Figures.Add(fillFigure);
        canvas.Children.Add(new Avalonia.Controls.Shapes.Path
        {
            Data = fillGeometry,
            Fill = new SolidColorBrush(Color.Parse("#10B981"), 0.15)
        });

        for (int i = 0; i < pts.Count - 1; i++)
        {
            double x1 = padL + i * xStep, y1 = YFor(pts[i].Amount);
            double x2 = padL + (i + 1) * xStep, y2 = YFor(pts[i + 1].Amount);
            canvas.Children.Add(new Line { StartPoint = new Point(x1, y1), EndPoint = new Point(x2, y2), Stroke = strokeBrush, StrokeThickness = 2 });
        }
        foreach (var (pt, i) in pts.Select((p, i) => (p, i)))
        {
            double x = padL + i * xStep, y = YFor(pt.Amount);
            var dot = new Ellipse { Width = 6, Height = 6, Fill = strokeBrush };
            Canvas.SetLeft(dot, x - 3); Canvas.SetTop(dot, y - 3);
            canvas.Children.Add(dot);

            var label = new TextBlock { Text = pt.Label, FontSize = 8, Foreground = Brushes.Gray };
            Canvas.SetLeft(label, x - 16); Canvas.SetTop(label, h - padB + 4);
            canvas.Children.Add(label);
        }
    }
}
