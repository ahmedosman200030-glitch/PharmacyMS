using Avalonia.Controls;

namespace PharmacyMS.Desktop.Views.Approvals;

public partial class RejectReasonDialog : Window
{
    public RejectReasonDialog()
    {
        InitializeComponent();
    }

    public RejectReasonDialog(string subjectDescription) : this()
    {
        InfoText.Text = $"Rejecting: {subjectDescription}. Please give a reason so the submitter knows what to fix.";

        CancelButton.Click += (_, _) => Close(null);
        ConfirmButton.Click += (_, _) =>
        {
            var reason = ReasonBox.Text?.Trim();
            if (string.IsNullOrEmpty(reason))
            {
                ErrorText.Text = "A reason is required.";
                ErrorText.IsVisible = true;
                return;
            }
            Close(reason);
        };
    }

    public static async System.Threading.Tasks.Task<string?> ShowAsync(Window owner, string subjectDescription)
    {
        var dialog = new RejectReasonDialog(subjectDescription);
        return await dialog.ShowDialog<string?>(owner);
    }
}
