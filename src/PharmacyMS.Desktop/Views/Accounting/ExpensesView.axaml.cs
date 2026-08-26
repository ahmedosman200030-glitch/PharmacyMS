using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Media;
using PharmacyMS.Application.Services;
using PharmacyMS.Desktop.ViewModels;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Desktop.Views.Accounting;

public partial class ExpensesView : UserControl
{
    private readonly ExpensesViewModel _vm;

    private static readonly string[] Palette =
    {
        "#DC2626", "#3B82F6", "#10B981", "#F59E0B", "#8B5CF6",
        "#EC4899", "#14B8A6", "#F97316", "#6366F1", "#84CC16"
    };

    public ExpensesView() { InitializeComponent(); }

    public ExpensesView(ExpensesViewModel vm)
    {
        InitializeComponent();
        _vm = vm;

        RangePicker.SetRange(_vm.FromDate, _vm.ToDate);

        CategoryFilterCombo.ItemsSource = new[] { "All" }.Concat(ExpensesViewModel.PredefinedCategories).ToList();
        CategoryFilterCombo.SelectedIndex = 0;

        RefreshButton.Click += async (_, _) =>
        {
            _vm.FromDate = RangePicker.FromDate;
            _vm.ToDate = RangePicker.ToDate;
            await LoadAndRender();
        };

        AddExpenseBtn.Click += async (_, _) =>
        {
            var owner = TopLevel.GetTopLevel(this) as Window;
            var dialog = new ExpenseFormWindow();
            if (owner != null)
                await dialog.ShowDialog(owner);
            else
                dialog.Show();

            if (dialog.Result != null)
            {
                await _vm.SubmitExpenseForApprovalAsync(dialog.Result);
                await LoadAndRender();
            }
        };

        CategoryFilterCombo.SelectionChanged += (_, _) =>
        {
            _vm.ActiveCategory = CategoryFilterCombo.SelectedItem as string ?? "All";
            _vm.CurrentPage = 1;
            RenderTable();
        };

        SearchBox.TextChanged += (_, _) => { _vm.SearchText = SearchBox.Text ?? string.Empty; _vm.CurrentPage = 1; RenderTable(); };

        PrevPageBtn.Click += (_, _) => { if (_vm.CurrentPage > 1) { _vm.CurrentPage--; RenderTable(); } };
        NextPageBtn.Click += (_, _) =>
        {
            var filtered = _vm.GetFilteredRows();
            if (_vm.CurrentPage < _vm.TotalPages(filtered)) { _vm.CurrentPage++; RenderTable(); }
        };

        ExpenseGrid.AddHandler(Button.ClickEvent, OnDeleteClick, Avalonia.Interactivity.RoutingStrategies.Bubble);
        ExpenseGrid.AddHandler(Button.ClickEvent, OnViewClick, Avalonia.Interactivity.RoutingStrategies.Bubble);

        DonutCanvas.SizeChanged += (_, _) => DrawDonut();
        TrendCanvas.SizeChanged += (_, _) => DrawTrend();

        AttachedToVisualTree += async (_, _) => await LoadAndRender();
    }

    private async void OnViewClick(object? sender, RoutedEventArgs e)
    {
        if (e.Source is Button { Name: "ViewBtn" } btn && btn.DataContext is Expense expense)
        {
            var owner = TopLevel.GetTopLevel(this) as Window;
            var detail = new TransactionDetailWindow();
            detail.Configure(
                icon: "💸",
                title: expense.Code,
                subtitle: expense.Category,
                accentHex: "#DC2626",
                rows: new (string, string)[]
                {
                    ("Date", expense.Date.ToString("yyyy-MM-dd")),
                    ("Category", expense.Category),
                    ("Description", expense.Description),
                    ("Amount", $"${expense.Amount:F2}"),
                    ("Payment Method", expense.PaymentMethod),
                    ("Added By", expense.CreatedBy),
                    ("Recorded At", expense.CreatedAt.ToString("yyyy-MM-dd HH:mm")),
                });
            if (owner != null) await detail.ShowDialog(owner); else detail.Show();
        }
    }

    private async void OnDeleteClick(object? sender, RoutedEventArgs e)
    {
        if (e.Source is Button { Name: "DeleteBtn" } btn && btn.DataContext is Expense expense)
        {
            if (!SessionManager.IsAdmin)
            {
                return;
            }

            await _vm.DeleteExpenseAsync(expense);
            await LoadAndRender();
        }
    }

    private async Task LoadAndRender()
    {
        await _vm.LoadAsync();
        RefreshStats();
        RenderTable();
        DrawDonut();
        DrawTrend();
        RenderTopCategories();
    }

    private void RefreshStats()
    {
        var period = $"{_vm.FromDate:MMM d} - {_vm.ToDate:MMM d}";

        TotalExpensesText.Text = $"${_vm.TotalExpenses:F2}";
        TotalExpensesPeriodText.Text = period;

        TxnCountText.Text = _vm.TransactionCount.ToString();
        TxnCountPeriodText.Text = period;

        AvgExpenseText.Text = $"${_vm.AverageExpense:F2}";
        AvgExpensePeriodText.Text = period;

        CategoryCountText.Text = _vm.CategoryCount.ToString();
        CategoryCountPeriodText.Text = period;
    }

    private void RenderTable()
    {
        var filtered = _vm.GetFilteredRows();
        var page = _vm.GetPage(filtered);
        ExpenseGrid.ItemsSource = page;

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

    private void RenderTopCategories()
    {
        TopCategoriesPanel.Children.Clear();
        var total = _vm.TotalExpenses == 0 ? 1 : _vm.TotalExpenses;

        foreach (var (cat, i) in _vm.CategoryTotals.Take(5).Select((c, i) => (c, i)))
        {
            var color = Palette[i % Palette.Length];
            var row = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8 };
            row.Children.Add(new Ellipse { Width = 8, Height = 8, Fill = new SolidColorBrush(Color.Parse(color)), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
            row.Children.Add(new TextBlock { Text = cat.Category, FontSize = 12, Foreground = new SolidColorBrush(Color.Parse("#334155")), Width = 90 });
            row.Children.Add(new TextBlock { Text = $"${cat.Amount:F2}", FontSize = 12, FontWeight = FontWeight.SemiBold, Foreground = new SolidColorBrush(Color.Parse("#0F172A")) });
            row.Children.Add(new TextBlock { Text = $"({cat.Amount / total:P0})", FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#94A3B8")) });
            TopCategoriesPanel.Children.Add(row);
        }

        if (_vm.CategoryTotals.Count == 0)
            TopCategoriesPanel.Children.Add(new TextBlock { Text = "No expenses yet", FontSize = 12, Foreground = Brushes.Gray });
    }

    private void DrawDonut()
    {
        var canvas = DonutCanvas;
        canvas.Children.Clear();
        double w = canvas.Bounds.Width > 0 ? canvas.Bounds.Width : 300;
        double h = canvas.Bounds.Height > 0 ? canvas.Bounds.Height : 160;
        double cx = w / 2 - 60, cy = h / 2, radius = Math.Min(h, w / 2) / 2 - 8, thickness = 18;

        var total = _vm.CategoryTotals.Sum(c => c.Amount);
        if (total <= 0)
        {
            var empty = new TextBlock { Text = "No expenses yet", Foreground = Brushes.Gray, FontSize = 12 };
            Canvas.SetLeft(empty, 10); Canvas.SetTop(empty, h / 2 - 8);
            canvas.Children.Add(empty);
            return;
        }

        double cursor = 0;
        int idx = 0;
        double legendY = cy - (Math.Min(_vm.CategoryTotals.Count, 5) * 24) / 2.0;
        foreach (var cat in _vm.CategoryTotals)
        {
            double frac = (double)(cat.Amount / total);
            var color = Palette[idx % Palette.Length];
            DrawArcSegment(canvas, cx, cy, radius, thickness, cursor * 360, (cursor + frac) * 360, Color.Parse(color));
            cursor += frac;

            if (idx < 5)
            {
                AddDonutLegendItem(canvas, w - 110, legendY + idx * 24, color, cat.Category, $"{frac:P0}");
            }
            idx++;
        }
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

        var brush = new SolidColorBrush(Color.Parse("#DC2626"));
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
