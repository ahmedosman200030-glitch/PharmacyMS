using System;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace PharmacyMS.Desktop.Views.Startup;

public partial class ServerUnreachableView : UserControl
{
    private Func<Task>? _onRetry;

    public ServerUnreachableView() { InitializeComponent(); }

    public ServerUnreachableView(string reason, Func<Task> onRetry)
    {
        InitializeComponent();
        _onRetry = onRetry;

        ReasonText.Text = $"Details: {reason}";

        RetryButton.Click += async (_, _) =>
        {
            RetryButton.IsEnabled = false;
            RetryButton.Content = "Retrying...";

            try
            {
                await _onRetry();
            }
            finally
            {
                // If retry succeeded, this control has already been replaced
                // by the next screen. If it's still visible, retry failed -
                // a fresh ServerUnreachableView will already be showing by
                // the time we get here, so re-enabling this instance is harmless.
                RetryButton.IsEnabled = true;
                RetryButton.Content = "Retry Connection";
            }
        };
    }
}
