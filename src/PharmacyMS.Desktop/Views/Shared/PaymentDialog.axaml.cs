using Avalonia.Controls;

namespace PharmacyMS.Desktop.Views.Shared;

public partial class PaymentDialog : Window
{
    public PaymentDialog()
    {
        InitializeComponent();
    }

    public PaymentDialog(string customerName, decimal balance) : this()
    {
        InfoText.Text = $"{customerName} owes ${balance:F2}. Enter the amount being paid now.";
        AmountBox.Text = balance.ToString("F2");

        CancelButton.Click += (_, _) => Close(null);
        ConfirmButton.Click += (_, _) =>
        {
            if (!decimal.TryParse(AmountBox.Text, out var amount) || amount <= 0)
            {
                ErrorText.Text = "Enter a valid amount greater than 0.";
                ErrorText.IsVisible = true;
                return;
            }
            if (amount > balance)
            {
                ErrorText.Text = $"Amount can't exceed the balance of ${balance:F2}.";
                ErrorText.IsVisible = true;
                return;
            }
            Close(amount);
        };
    }

    public static async System.Threading.Tasks.Task<decimal?> ShowAsync(Window owner, string customerName, decimal balance)
    {
        var dialog = new PaymentDialog(customerName, balance);
        return await dialog.ShowDialog<decimal?>(owner);
    }
}
