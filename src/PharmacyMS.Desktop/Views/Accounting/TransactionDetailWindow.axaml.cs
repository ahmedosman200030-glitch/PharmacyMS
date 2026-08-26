using Avalonia.Controls;
using Avalonia.Media;

namespace PharmacyMS.Desktop.Views.Accounting;

public partial class TransactionDetailWindow : Window
{
    public TransactionDetailWindow()
    {
        InitializeComponent();
        CloseButton.Click += (_, _) => Close();
        CloseFooterButton.Click += (_, _) => Close();
    }

    /// <param name="accentHex">Header background color, e.g. "#DC2626" for expenses, "#2563EB" for income.</param>
    public void Configure(string icon, string title, string subtitle, string accentHex, (string Label, string Value)[] rows)
    {
        HeaderIcon.Text = icon;
        HeaderTitle.Text = title;
        HeaderSubtitle.Text = subtitle;
        HeaderBorder.Background = new SolidColorBrush(Color.Parse(accentHex));

        RowsPanel.Children.Clear();
        foreach (var (label, value) in rows)
        {
            var panel = new StackPanel { Spacing = 4 };
            panel.Children.Add(new TextBlock { Text = label.ToUpperInvariant(), Classes = { "rowLabel" } });
            panel.Children.Add(new TextBlock { Text = string.IsNullOrWhiteSpace(value) ? "—" : value, Classes = { "rowValue" } });
            RowsPanel.Children.Add(panel);
            RowsPanel.Children.Add(new Border { Height = 1, Background = new SolidColorBrush(Color.Parse("#E2E8F0")), Margin = new Avalonia.Thickness(0, 4, 0, 0) });
        }
    }
}
