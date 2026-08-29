using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;

namespace PharmacyMS.Desktop.Views.Splash;

public partial class SplashView : UserControl
{
    public SplashView()
    {
        InitializeComponent();
    }

    public async Task RunAsync(int durationSeconds, Action onFinished)
    {
        var steps = durationSeconds * 10;
        for (int i = 1; i <= steps; i++)
        {
            await Task.Delay(100).ConfigureAwait(false);
            var percent = (int)((i / (double)steps) * 100);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                SplashProgressBar.Value = percent;
                PercentText.Text = $"{percent}%";
            });
        }

        await Dispatcher.UIThread.InvokeAsync(onFinished);
    }
}
