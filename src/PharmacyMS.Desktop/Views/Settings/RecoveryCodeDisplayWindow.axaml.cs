using Avalonia.Controls;
using Avalonia.Input.Platform;

namespace PharmacyMS.Desktop.Views.Settings;

public partial class RecoveryCodeDisplayWindow : Window
{
    public RecoveryCodeDisplayWindow() { InitializeComponent(); }

    public RecoveryCodeDisplayWindow(string code)
    {
        InitializeComponent();
        CodeBox.Text = code;

        CopyButton.Click += async (_, _) =>
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
            {
                await clipboard.SetTextAsync(code);
                CopiedText.IsVisible = true;
            }
        };

        DoneButton.Click += (_, _) => Close();
    }
}
