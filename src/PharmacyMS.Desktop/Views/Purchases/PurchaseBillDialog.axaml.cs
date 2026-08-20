using Avalonia.Controls;
using Avalonia.Media;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Desktop.Views.Purchases;

public partial class PurchaseBillDialog : Window
{
    public PurchaseBillDialog() { InitializeComponent(); }

    public PurchaseBillDialog(Purchase detail) : this()
    {
        BillNoText.Text = $"SB-{detail.Id:D6}";
        DateText.Text = detail.CreatedAt.ToString("dd-MMM-yyyy");
        SupplierText.Text = detail.SupplierName;

        StatusText.Text = detail.Status.ToString();
        StatusText.Foreground = detail.DueAmount > 0
            ? new SolidColorBrush(Color.Parse("#DC2626"))
            : new SolidColorBrush(Color.Parse("#1D9E75"));

        ItemsGrid.ItemsSource = detail.Items;

        TotalText.Text = $"${detail.TotalAmount:F2}";
        PaidText.Text = $"${detail.AmountPaid:F2}";
        DueText.Text = $"${detail.DueAmount:F2}";

        CloseButton.Click += (_, _) => Close();
    }
}
