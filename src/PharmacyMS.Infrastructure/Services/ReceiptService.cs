using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Application.Localization;
using PharmacyMS.Application.Interfaces.Services;
using PharmacyMS.Domain.Entities;
using PharmacyMS.Domain.Enums;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PharmacyMS.Infrastructure.Services;

public class ReceiptService : IReceiptService
{
    private readonly IBrandingService _brandingService;
    private readonly IAppSettingsService _settingsService;
    private readonly IUserRepository _userRepository;

    public ReceiptService(IBrandingService brandingService, IAppSettingsService settingsService, IUserRepository userRepository)
    {
        _brandingService = brandingService;
        _settingsService = settingsService;
        _userRepository = userRepository;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<ReceiptData> BuildReceiptAsync(Sale sale, string customerName,
        string paymentMethod, decimal amountReceived, decimal change, decimal totalDiscount)
    {
        var branding = await _brandingService.GetAsync();
        var footer = await _settingsService.GetReceiptFooterAsync();
        var currency = await _settingsService.GetCurrencySymbolAsync();
        var cashierName = await ResolveCashierNameAsync(sale.CashierId);
        var slshRate = await _settingsService.GetSlshExchangeRateAsync();
        var lang = await _settingsService.GetLanguageAsync();

        return new ReceiptData
        {
            PharmacyName = !string.IsNullOrWhiteSpace(branding.PharmacyName) ? branding.PharmacyName : branding.AppName,
            Tagline = branding.Tagline,
            LogoPath = branding.LogoPath,
            Address = branding.Address ?? "",
            Phone = branding.PhoneNumber ?? "",
            Phone2 = branding.MobileNumber,
            Email = branding.Email,
            ContactNumber = branding.ContactNumber,
            InvoiceNumber = sale.InvoiceNumber,
            DateTime = sale.CreatedAt,
            CashierName = cashierName,
            CustomerName = customerName,
            Items = sale.Items.Select(i => new ReceiptLine
            {
                Name = i.MedicineName,
                Unit = i.Unit,
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
            SlshTotal = slshRate > 0 ? sale.TotalAmount * slshRate : null,
            Language = lang,
            PaymentMethod = paymentMethod,
            AmountReceived = amountReceived,
            Change = change,
            Footer = footer
        };
    }

    public Task PrintAsync(ReceiptData receipt)
    {
        // Printing is handled in the UI layer (ReceiptWindow opens the generated PDF)
        return Task.CompletedTask;
    }

    public async Task<string> SaveAsPdfAsync(ReceiptData receipt)
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PharmaPro", "Receipts");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, $"Receipt-{receipt.InvoiceNumber}.pdf");

        var qrBytes = GenerateQrCodeBytes(receipt);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.ContinuousSize(320);
                page.MarginVertical(20);
                page.MarginHorizontal(18);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Helvetica"));

                page.Content().Column(col =>
                {
                    // Logo
                    if (!string.IsNullOrWhiteSpace(receipt.LogoPath) && File.Exists(receipt.LogoPath))
                    {
                        col.Item().AlignCenter().Height(50).Image(receipt.LogoPath).FitArea();
                        col.Item().Height(6);
                    }

                    // Pharmacy name
                    col.Item().AlignCenter().Text(receipt.PharmacyName)
                        .FontSize(15).Bold().FontColor(Colors.Black);

                    // Tagline
                    if (!string.IsNullOrWhiteSpace(receipt.Tagline))
                        col.Item().AlignCenter().Text(receipt.Tagline)
                            .FontSize(8.5f).Italic().FontColor(Colors.Grey.Medium);

                    col.Item().Height(4);

                    if (!string.IsNullOrWhiteSpace(receipt.Address))
                        col.Item().AlignCenter().Text(receipt.Address).FontSize(8.5f).FontColor(Colors.Grey.Darken1);

                    if (!string.IsNullOrWhiteSpace(receipt.Phone))
                        col.Item().AlignCenter().Text($"ZAAD: {receipt.Phone}").FontSize(8.5f).SemiBold();

                    if (!string.IsNullOrWhiteSpace(receipt.Phone2))
                        col.Item().AlignCenter().Text($"E-DAHAB: {receipt.Phone2}").FontSize(8.5f).SemiBold();

                    if (!string.IsNullOrWhiteSpace(receipt.Email))
                        col.Item().AlignCenter().Text(receipt.Email).FontSize(8.5f).FontColor(Colors.Grey.Darken1);

                    if (!string.IsNullOrWhiteSpace(receipt.ContactNumber))
                        col.Item().AlignCenter().Text($"Contact: {receipt.ContactNumber}").FontSize(8.5f).SemiBold().FontColor(Colors.Grey.Darken2);

                    col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    // Invoice meta
                    col.Item().Column(meta =>
                    {
                        meta.Item().Row(r => { r.RelativeItem().Text(AppStrings.Get("Invoice", receipt.Language) + ":").FontColor(Colors.Grey.Darken1); r.RelativeItem(2).AlignRight().Text(receipt.InvoiceNumber).Bold(); });
                        meta.Item().Row(r => { r.RelativeItem().Text(AppStrings.Get("Date", receipt.Language) + ":").FontColor(Colors.Grey.Darken1); r.RelativeItem(2).AlignRight().Text(receipt.DateTime.ToString("dd MMM yyyy  HH:mm")); });
                        meta.Item().Row(r => { r.RelativeItem().Text(AppStrings.Get("Cashier", receipt.Language) + ":").FontColor(Colors.Grey.Darken1); r.RelativeItem(2).AlignRight().Text(receipt.CashierName); });
                        meta.Item().Row(r => { r.RelativeItem().Text(AppStrings.Get("Customer", receipt.Language) + ":").FontColor(Colors.Grey.Darken1); r.RelativeItem(2).AlignRight().Text(receipt.CustomerName); });
                    });

                    col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    // Items table
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(4);
                            c.RelativeColumn(1);
                            c.RelativeColumn(1);
                            c.RelativeColumn(1.5f);
                            c.RelativeColumn(1.5f);
                        });

                        table.Header(h =>
                        {
                            h.Cell().Text(AppStrings.Get("Item", receipt.Language)).FontSize(8).Bold().FontColor(Colors.Grey.Darken1);
                            h.Cell().AlignCenter().Text(AppStrings.Get("Qty", receipt.Language)).FontSize(8).Bold().FontColor(Colors.Grey.Darken1);
                            h.Cell().AlignCenter().Text("Unit").FontSize(8).Bold().FontColor(Colors.Grey.Darken1);
                            h.Cell().AlignRight().Text(AppStrings.Get("Price", receipt.Language)).FontSize(8).Bold().FontColor(Colors.Grey.Darken1);
                            h.Cell().AlignRight().Text(AppStrings.Get("Total", receipt.Language)).FontSize(8).Bold().FontColor(Colors.Grey.Darken1);
                            h.Cell().ColumnSpan(5).PaddingTop(3).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                        });

                        foreach (var item in receipt.Items)
                        {
                            table.Cell().PaddingVertical(2).Text(item.Name).FontSize(9);
                            table.Cell().PaddingVertical(2).AlignCenter().Text(item.Quantity.ToString()).FontSize(9);
                            table.Cell().PaddingVertical(2).AlignCenter().Text(item.Unit).FontSize(9);
                            table.Cell().PaddingVertical(2).AlignRight().Text(item.UnitPrice.ToString("F2")).FontSize(9);
                            table.Cell().PaddingVertical(2).AlignRight().Text(item.LineTotal.ToString("F2")).FontSize(9);
                        }
                    });

                    col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    // Totals
                    col.Item().Column(totals =>
                    {
                        totals.Item().Row(r => { r.RelativeItem().Text(AppStrings.Get("Subtotal", receipt.Language)).FontColor(Colors.Grey.Darken1); r.RelativeItem().AlignRight().Text(receipt.Subtotal.ToString("F2")); });
                        if (receipt.TotalDiscount > 0)
                            totals.Item().Row(r => { r.RelativeItem().Text(AppStrings.Get("Discount", receipt.Language)).FontColor(Colors.Red.Medium); r.RelativeItem().AlignRight().Text($"-{receipt.TotalDiscount:F2}").FontColor(Colors.Red.Medium); });
                        totals.Item().Row(r => { r.RelativeItem().Text($"{AppStrings.Get("Tax", receipt.Language)} ({receipt.TaxPercent:0.##}%)").FontColor(Colors.Grey.Darken1); r.RelativeItem().AlignRight().Text(receipt.TaxAmount.ToString("F2")); });
                    });

                    col.Item().PaddingVertical(6).LineHorizontal(1).LineColor(Colors.Black);

                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text(AppStrings.Get("TOTAL", receipt.Language)).FontSize(12).Bold();
                        r.RelativeItem().AlignRight().Text(receipt.Total.ToString("F2")).FontSize(12).Bold();
                    });

                    if (receipt.SlshTotal.HasValue)
                        col.Item().AlignRight().Text($"{receipt.SlshTotal.Value:N0} SLSH")
                            .FontSize(9.5f).SemiBold().FontColor(Colors.Grey.Darken2);

                    col.Item().PaddingTop(4).Row(r =>
                    {
                        r.RelativeItem().Text($"{AppStrings.Get("Payment", receipt.Language)} ({receipt.PaymentMethod})").FontColor(Colors.Grey.Darken1);
                        r.RelativeItem().AlignRight().Text(receipt.AmountReceived.ToString("F2"));
                    });
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text(AppStrings.Get("Change", receipt.Language)).FontColor(Colors.Green.Darken1).Bold();
                        r.RelativeItem().AlignRight().Text(receipt.Change.ToString("F2")).FontColor(Colors.Green.Darken1).Bold();
                    });

                    col.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    // QR code
                    if (qrBytes != null)
                        col.Item().AlignCenter().Width(90).Height(90).Image(qrBytes);

                    col.Item().PaddingTop(8).AlignCenter().Text(receipt.Footer)
                        .FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                });
            });
        });

        document.GeneratePdf(path);
        return await Task.FromResult(path);
    }

    private async Task<string> ResolveCashierNameAsync(int cashierId)
    {
        var user = await _userRepository.GetByIdAsync(cashierId);
        if (user == null) return "Unknown";
        return user.Role == UserRole.Admin ? "Admin System" : user.FullName;
    }

    private static byte[]? GenerateQrCodeBytes(ReceiptData r)
    {
        try
        {
            var qrText = $"Invoice:{r.InvoiceNumber}|Total:{r.Total:F2}|Date:{r.DateTime:yyyyMMddHHmm}";
            using var qrGenerator = new QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(qrText, QRCodeGenerator.ECCLevel.M);
            using var qrCode = new PngByteQRCode(qrData);
            return qrCode.GetGraphic(6);
        }
        catch
        {
            return null;
        }
    }
}
