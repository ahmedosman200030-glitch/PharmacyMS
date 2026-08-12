using System.Globalization;
using Avalonia.Controls;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Desktop.Views.Purchases;

public partial class PurchasePaymentDialog : Window
{
    public decimal? ResultAmount { get; private set; }

    public PurchasePaymentDialog() { InitializeComponent(); }

    public PurchasePaymentDialog(Purchase purchase)
    {
        InitializeComponent();

        SupplierText.Text = purchase.SupplierName;
        DueText.Text = $"Total: ${purchase.TotalAmount:F2}   Paid: ${purchase.AmountPaid:F2}   Due: ${purchase.DueAmount:F2}";
        AmountBox.Text = purchase.DueAmount.ToString("F2", CultureInfo.InvariantCulture);

        CancelButton.Click += (_, _) => Close();

        ConfirmButton.Click += (_, _) =>
        {
            if (!decimal.TryParse(AmountBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount) || amount <= 0)
            {
                ErrorText.Text = "Enter a valid amount.";
                ErrorText.IsVisible = true;
                return;
            }

            if (amount > purchase.DueAmount)
            {
                ErrorText.Text = $"Amount cannot exceed due balance (${purchase.DueAmount:F2}).";
                ErrorText.IsVisible = true;
                return;
            }

            ResultAmount = amount;
            Close();
        };
    }
}
