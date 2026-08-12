using PharmacyMS.Application.Interfaces.Services;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Infrastructure.Services;

public class ReceiptService : IReceiptService
{
    private readonly IBrandingService _brandingService;
    private readonly IAppSettingsService _settingsService;

    public ReceiptService(IBrandingService brandingService, IAppSettingsService settingsService)
    {
        _brandingService = brandingService;
        _settingsService = settingsService;
    }

    public async Task<ReceiptData> BuildReceiptAsync(Sale sale, string customerName,
        string paymentMethod, decimal amountReceived, decimal change, decimal totalDiscount)
    {
        var branding = await _brandingService.GetAsync();
        var footer = await _settingsService.GetReceiptFooterAsync();
        var currency = await _settingsService.GetCurrencySymbolAsync();

        return new ReceiptData
        {
            PharmacyName = branding.AppName,
            LogoPath = branding.LogoPath,
            InvoiceNumber = sale.InvoiceNumber,
            DateTime = sale.CreatedAt,
            CashierName = sale.CashierId.ToString(),
            CustomerName = customerName,
            Items = sale.Items.Select(i => new ReceiptLine
            {
                Name = i.MedicineName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Discount = 0,
                LineTotal = i.UnitPrice * i.Quantity
            }).ToList(),
            Subtotal = sale.Subtotal,
            TotalDiscount = totalDiscount,
            TaxPercent = sale.TaxRate * 100,
            TaxAmount = sale.TaxAmount,
            Total = sale.TotalAmount,
            PaymentMethod = paymentMethod,
            AmountReceived = amountReceived,
            Change = change,
            Footer = footer
        };
    }

    public Task PrintAsync(ReceiptData receipt)
    {
        // Printing is handled in the UI layer (ReceiptWindow shows a printable view)
        return Task.CompletedTask;
    }

    public async Task<string> SaveAsPdfAsync(ReceiptData receipt)
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PharmacyMS", "Receipts");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, $"Receipt-{receipt.InvoiceNumber}.txt");
        await File.WriteAllTextAsync(path, BuildTextReceipt(receipt));
        return path;
    }

    private static string BuildTextReceipt(ReceiptData r)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(r.PharmacyName.PadLeft(25));
        if (!string.IsNullOrWhiteSpace(r.Address)) sb.AppendLine(r.Address.PadLeft(25));
        if (!string.IsNullOrWhiteSpace(r.Phone)) sb.AppendLine(r.Phone.PadLeft(25));
        sb.AppendLine(new string('-', 40));
        sb.AppendLine($"Invoice : {r.InvoiceNumber}");
        sb.AppendLine($"Date    : {r.DateTime:dd MMM yyyy HH:mm}");
        sb.AppendLine($"Cashier : {r.CashierName}");
        sb.AppendLine($"Customer: {r.CustomerName}");
        sb.AppendLine(new string('-', 40));
        foreach (var item in r.Items)
            sb.AppendLine($"{item.Name,-20} {item.Quantity,3} x {item.UnitPrice,7:F2} = {item.LineTotal,8:F2}");
        sb.AppendLine(new string('-', 40));
        sb.AppendLine($"{"Subtotal",-30} {r.Subtotal,8:F2}");
        if (r.TotalDiscount > 0) sb.AppendLine($"{"Discount",-30} -{r.TotalDiscount,7:F2}");
        sb.AppendLine($"{"Tax (" + r.TaxPercent + "%)",-30} {r.TaxAmount,8:F2}");
        sb.AppendLine($"{"TOTAL",-30} {r.Total,8:F2}");
        sb.AppendLine($"{"Payment (" + r.PaymentMethod + ")",-30} {r.AmountReceived,8:F2}");
        sb.AppendLine($"{"Change",-30} {r.Change,8:F2}");
        sb.AppendLine(new string('-', 40));
        sb.AppendLine(r.Footer.PadLeft(25));
        return sb.ToString();
    }
}
