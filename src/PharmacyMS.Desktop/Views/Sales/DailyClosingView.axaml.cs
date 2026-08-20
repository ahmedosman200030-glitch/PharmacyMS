using Avalonia.Controls;
using PharmacyMS.Desktop.ViewModels;

namespace PharmacyMS.Desktop.Views.Sales;

public partial class DailyClosingView : UserControl
{
    private readonly DailyClosingViewModel _vm;

    public DailyClosingView() { InitializeComponent(); }
    public DailyClosingView(DailyClosingViewModel vm)
    {
        InitializeComponent();
        _vm = vm;

        HistoryGrid.ItemsSource = _vm.History;
        HistoryGrid.LoadingRow += (_, e) => e.Row.Header = (e.Row.GetIndex() + 1).ToString();

        AttachedToVisualTree += async (_, _) => await RefreshAsync();

        ActualCashBox.PropertyChanged += (_, e) =>
        {
            if (e.Property == TextBox.TextProperty) UpdateDifferencePreview();
        };

        CloseRegisterButton.Click += async (_, _) =>
        {
            if (!decimal.TryParse(ActualCashBox.Text, out var actualCash) || actualCash < 0)
            {
                StatusText.Foreground = Avalonia.Media.Brush.Parse("#EF4444");
                StatusText.Text = "Enter a valid cash amount.";
                return;
            }

            var closing = await _vm.CloseRegisterAsync(actualCash, string.IsNullOrWhiteSpace(NotesBox.Text) ? null : NotesBox.Text);
            StatusText.Foreground = Avalonia.Media.Brush.Parse("#22C55E");
            StatusText.Text = $"Register closed. Difference: ${closing.Difference:F2}.";
            await RefreshAsync();
        };
    }

    private void UpdateDifferencePreview()
    {
        if (decimal.TryParse(ActualCashBox.Text, out var actual))
        {
            var diff = actual - _vm.ExpectedCash;
            DifferenceText.Text = $"Difference: ${diff:F2}";
            DifferenceText.Foreground = diff == 0
                ? Avalonia.Media.Brush.Parse("#22C55E")
                : Avalonia.Media.Brush.Parse("#EF4444");
        }
        else
        {
            DifferenceText.Text = "";
        }
    }

    private async Task RefreshAsync()
    {
        await _vm.LoadAsync();

        CashSalesText.Text = $"${_vm.CashSales:F2}";
        CardSalesText.Text = $"${_vm.CardSales:F2}";
        MobileSalesText.Text = $"${_vm.MobileSales:F2}";
        InsuranceSalesText.Text = $"${_vm.InsuranceSales:F2}";

        ClosingFormBorder.IsVisible = !_vm.AlreadyClosedToday;
        AlreadyClosedBorder.IsVisible = _vm.AlreadyClosedToday;

        ActualCashBox.Text = "";
        NotesBox.Text = "";
        DifferenceText.Text = "";
    }
}
