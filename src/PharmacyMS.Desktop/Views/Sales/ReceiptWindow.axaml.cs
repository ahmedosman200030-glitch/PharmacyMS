using Avalonia.Controls;
using Avalonia.Media.Imaging;
using PharmacyMS.Application.Interfaces.Services;
using QRCoder;

namespace PharmacyMS.Desktop.Views.Sales;

public partial class ReceiptWindow : Window
{
    private readonly ReceiptData _receipt;
    private readonly IReceiptService _receiptService;

    public ReceiptWindow(ReceiptData receipt, IReceiptService receiptService)
    {
        InitializeComponent();
        _receipt = receipt;
        _receiptService = receiptService;

        PopulateReceipt();

        PrintButton.Click += async (_, _) => await PrintAsync();
        SaveButton.Click += async (_, _) => await SaveAsync();
        CloseButton.Click += (_, _) => Close();
    }

    private void PopulateReceipt()
    {
        // Logo
        if (!string.IsNullOrWhiteSpace(_receipt.LogoPath) && File.Exists(_receipt.LogoPath))
        {
            LogoImage.Source = new Bitmap(_receipt.LogoPath);
            LogoImage.IsVisible = true;
        }

        PharmacyNameText.Text = _receipt.PharmacyName;

        if (!string.IsNullOrWhiteSpace(_receipt.Tagline))
        {
            TaglineText.Text = _receipt.Tagline;
            TaglineText.IsVisible = true;
        }

        AddressText.Text = _receipt.Address;

        if (!string.IsNullOrWhiteSpace(_receipt.Phone))
        {
            PhoneText.Text = $"ZAAD: {_receipt.Phone}";
            PhoneText.IsVisible = true;
        }
        else
        {
            PhoneText.IsVisible = false;
        }

        if (!string.IsNullOrWhiteSpace(_receipt.Phone2))
        {
            Phone2Text.Text = $"E-DAHAB: {_receipt.Phone2}";
            Phone2Text.IsVisible = true;
        }

        if (!string.IsNullOrWhiteSpace(_receipt.Email))
        {
            EmailText.Text = _receipt.Email;
            EmailText.IsVisible = true;
        }

        if (!string.IsNullOrWhiteSpace(_receipt.ContactNumber))
        {
            ContactNumberText.Text = $"Contact: {_receipt.ContactNumber}";
            ContactNumberText.IsVisible = true;
        }

        InvoiceText.Text = _receipt.InvoiceNumber;
        DateText.Text = _receipt.DateTime.ToString("dd MMM yyyy  HH:mm");
        CashierText.Text = _receipt.CashierName;
        CustomerText.Text = _receipt.CustomerName;

        ItemsList.ItemsSource = _receipt.Items;

        SubtotalText.Text = $"${_receipt.Subtotal:F2}";

        if (_receipt.TotalDiscount > 0)
        {
            DiscountText.Text = $"-${_receipt.TotalDiscount:F2}";
            DiscountRow.IsVisible = true;
        }
        else
        {
            DiscountRow.IsVisible = false;
        }

        TaxLabel.Text = $"Tax ({_receipt.TaxPercent:F0}%)";
        TaxText.Text = $"${_receipt.TaxAmount:F2}";
        TotalText.Text = $"${_receipt.Total:F2}";

        if (_receipt.SlshTotal.HasValue)
        {
            SlshText.Text = $"{_receipt.SlshTotal.Value:N0} SLSH";
            SlshText.IsVisible = true;
        }
        PaymentLabel.Text = $"Payment ({_receipt.PaymentMethod})";
        PaidText.Text = $"${_receipt.AmountReceived:F2}";
        ChangeText.Text = $"${_receipt.Change:F2}";

        FooterText.Text = _receipt.Footer;

        // QR Code — encodes the invoice number + total
        GenerateQrCode();
    }

    private void GenerateQrCode()
    {
        try
        {
            var qrText = $"Invoice:{_receipt.InvoiceNumber}|Total:{_receipt.Total:F2}|Date:{_receipt.DateTime:yyyyMMddHHmm}";
            using var qrGenerator = new QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(qrText, QRCodeGenerator.ECCLevel.M);
            using var qrCode = new PngByteQRCode(qrData);
            var pngBytes = qrCode.GetGraphic(4);
            using var ms = new MemoryStream(pngBytes);
            QrImage.Source = new Bitmap(ms);
        }
        catch
        {
            QrImage.IsVisible = false;
        }
    }

    private async Task PrintAsync()
    {
        // Avalonia doesn't have built-in print — open a basic print dialog via OS
        // For now save and open with default viewer (which handles print)
        var path = await _receiptService.SaveAsPdfAsync(_receipt);
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true }); }
        catch { /* ignore */ }
    }

    private async Task SaveAsync()
    {
        var path = await _receiptService.SaveAsPdfAsync(_receipt);
        FooterText.Text = $"Saved to: {path}";
    }
}
