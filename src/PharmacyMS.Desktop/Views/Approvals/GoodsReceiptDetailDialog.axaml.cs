using Avalonia.Controls;
using PharmacyMS.Domain.Entities;
using PharmacyMS.Domain.Enums;

namespace PharmacyMS.Desktop.Views.Approvals;

public partial class GoodsReceiptDetailDialog : Window
{
    public GoodsReceiptDetailDialog()
    {
        InitializeComponent();
    }

    public GoodsReceiptDetailDialog(GoodsReceipt receipt) : this()
    {
        PoNumberText.Text = $"#{receipt.PurchaseOrderId}";
        ReceivedAtText.Text = receipt.ReceivedAt.ToString("yyyy-MM-dd HH:mm");
        SubmittedByText.Text = string.IsNullOrWhiteSpace(receipt.ReceivedByUserName) ? "Unknown" : receipt.ReceivedByUserName;
        TotalText.Text = $"${receipt.TotalAmount:F2}";
        NotesText.Text = string.IsNullOrWhiteSpace(receipt.Notes) ? "—" : receipt.Notes;

        if (receipt.ApprovalStatus == ApprovalStatus.Rejected && !string.IsNullOrWhiteSpace(receipt.RejectionReason))
        {
            RejectionBox.IsVisible = true;
            RejectionReasonText.Text = receipt.RejectionReason;
        }

        ItemsGrid.ItemsSource = receipt.Items;
        CloseButton.Click += (_, _) => Close();
    }

    public static async System.Threading.Tasks.Task ShowAsync(Window owner, GoodsReceipt receipt)
    {
        var dialog = new GoodsReceiptDetailDialog(receipt);
        await dialog.ShowDialog(owner);
    }
}
