using Avalonia.Controls;

namespace PharmacyMS.Desktop.Views.Shared;

public partial class ConfirmDialog : Window
{
    public ConfirmDialog()
    {
        InitializeComponent();
    }

    public ConfirmDialog(string title, string message) : this()
    {
        Title = title;
        MessageText.Text = message;
        YesButton.Click += (_, _) => Close(true);
        NoButton.Click += (_, _) => Close(false);
    }

    public static async System.Threading.Tasks.Task<bool> ShowAsync(Window owner, string title, string message)
    {
        var dialog = new ConfirmDialog(title, message);
        var result = await dialog.ShowDialog<bool?>(owner);
        return result == true;
    }
}
