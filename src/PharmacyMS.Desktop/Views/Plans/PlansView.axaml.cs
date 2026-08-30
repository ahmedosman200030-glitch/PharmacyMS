using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace PharmacyMS.Desktop.Views.Plans;

public partial class PlansView : UserControl
{
    // Wiring for what each button should actually do (e.g. move to next
    // wizard step, save chosen plan, kick off trial vs paid flow) is TBD -
    // for now these just invoke whichever callback you pass in.
    private readonly Action? _onSelectTrial;
    private readonly Action? _onSelectMonthly;
    private readonly Action? _onSelectAnnual;

    public PlansView(Action? onSelectTrial = null, Action? onSelectMonthly = null, Action? onSelectAnnual = null)
    {
        AvaloniaXamlLoader.Load(this);
        _onSelectTrial = onSelectTrial;
        _onSelectMonthly = onSelectMonthly;
        _onSelectAnnual = onSelectAnnual;
    }

    private void OnStartTrialClick(object? sender, RoutedEventArgs e)
    {
        _onSelectTrial?.Invoke();
    }

    private void OnChooseMonthlyClick(object? sender, RoutedEventArgs e)
    {
        _onSelectMonthly?.Invoke();
    }

    private void OnChooseAnnualClick(object? sender, RoutedEventArgs e)
    {
        _onSelectAnnual?.Invoke();
    }
}
