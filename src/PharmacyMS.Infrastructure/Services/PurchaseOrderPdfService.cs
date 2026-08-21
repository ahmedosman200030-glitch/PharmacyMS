using PharmacyMS.Application.Interfaces.Services;
using PharmacyMS.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PharmacyMS.Infrastructure.Services;

public class PurchaseOrderPdfService : IPurchaseOrderPdfService
{
    private readonly IBrandingService _brandingService;

    public PurchaseOrderPdfService(IBrandingService brandingService)
    {
        _brandingService = brandingService;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<string> GeneratePdfAsync(PurchaseOrder order, Supplier supplier)
    {
        var b = await _brandingService.GetAsync();
        var pharmacyName = !string.IsNullOrWhiteSpace(b.PharmacyName) ? b.PharmacyName : b.AppName;
        var address  = b.Address ?? "";
        var phone    = !string.IsNullOrWhiteSpace(b.ContactNumber) ? b.ContactNumber
                     : !string.IsNullOrWhiteSpace(b.PhoneNumber) ? b.PhoneNumber
                     : b.MobileNumber ?? "";
        var mobile   = b.MobileNumber ?? "";
        var email    = b.Email ?? "";
        var logoPath = b.LogoPath ?? "";
        var printedBy = PharmacyMS.Application.Services.SessionManager.CurrentUser?.FullName ?? "System";

        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PharmaPro", "PurchaseOrders");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, $"{order.OrderNumber}.pdf");

        var orderDate    = order.CreatedAt == default ? DateTime.Now : order.CreatedAt;
        var deliveryDate = order.ExpectedDate ?? orderDate.AddDays(4);
        var subTotal     = order.TotalAmount;

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9.5f).FontFamily("Helvetica"));

                // ── HEADER ────────────────────────────────────────────────
                page.Header().Column(col =>
                {
                    // Top row: logo+name  |  PURCHASE ORDER title
                    col.Item().Row(row =>
                    {
                        // Left: logo + pharmacy info
                        row.RelativeItem().Column(lc =>
                        {
                            if (!string.IsNullOrWhiteSpace(logoPath) && File.Exists(logoPath))
                                lc.Item().Width(55).Image(logoPath).FitWidth();

                            lc.Item().Text(pharmacyName)
                                .FontSize(15).Bold().FontColor("#C0392B");

                            if (!string.IsNullOrWhiteSpace(address))
                                lc.Item().Text(address).FontSize(8).FontColor("#555555");

                            var contact = new List<string>();
                            if (!string.IsNullOrWhiteSpace(phone))  contact.Add($"Tel: {phone}");
                            if (!string.IsNullOrWhiteSpace(mobile)) contact.Add($"Mobile: {mobile}");
                            if (!string.IsNullOrWhiteSpace(email))  contact.Add($"Email: {email}");
                            if (contact.Any())
                                lc.Item().Text(string.Join("  |  ", contact))
                                    .FontSize(8).FontColor("#555555");
                        });

                        // Right: title + meta
                        row.ConstantItem(190).Column(rc =>
                        {
                            rc.Item().AlignRight().Text("PURCHASE ORDER")
                                .FontSize(17).Bold().FontColor("#0F172A");
                            rc.Item().PaddingTop(6).AlignRight()
                                .Text($"PO No      :  {order.OrderNumber}")
                                .FontSize(9).Bold().FontColor("#C0392B");
                            rc.Item().AlignRight()
                                .Text($"Date         :  {orderDate:dd-MMM-yyyy}")
                                .FontSize(9);
                            rc.Item().AlignRight()
                                .Text($"Delivery    :  {deliveryDate:dd-MMM-yyyy}")
                                .FontSize(9);
                        });
                    });

                    col.Item().PaddingTop(8).LineHorizontal(1).LineColor("#CCCCCC");
                });

                // ── CONTENT ───────────────────────────────────────────────
                page.Content().PaddingTop(10).Column(col =>
                {
                    // Supplier + Ship To
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Border(1).BorderColor("#CCCCCC").Column(c =>
                        {
                            c.Item().Background("#EEEEEE").Padding(5)
                                .Text("Supplier Details").Bold().FontSize(9);
                            c.Item().Padding(8).Column(sc =>
                            {
                                sc.Item().Text(supplier.Name).Bold();
                                if (!string.IsNullOrWhiteSpace(supplier.Address))
                                    sc.Item().Text(supplier.Address!).FontColor("#444444");
                                if (!string.IsNullOrWhiteSpace(supplier.Phone))
                                    sc.Item().Text("Tel: " + supplier.Phone).FontColor("#444444");
                                if (!string.IsNullOrWhiteSpace(supplier.Email))
                                    sc.Item().Text("Email: " + supplier.Email).FontColor("#444444");
                                if (!string.IsNullOrWhiteSpace(supplier.ContactPerson))
                                    sc.Item().Text("Attn: " + supplier.ContactPerson).FontColor("#444444");
                            });
                        });

                        row.ConstantItem(14);

                        row.RelativeItem().Border(1).BorderColor("#CCCCCC").Column(c =>
                        {
                            c.Item().Background("#EEEEEE").Padding(5)
                                .Text("Ship To").Bold().FontSize(9);
                            c.Item().Padding(8).Column(sc =>
                            {
                                sc.Item().Text(pharmacyName).Bold();
                                if (!string.IsNullOrWhiteSpace(address))
                                    sc.Item().Text(address).FontColor("#444444");
                                if (!string.IsNullOrWhiteSpace(phone))
                                    sc.Item().Text("Tel: " + phone).FontColor("#444444");
                                if (!string.IsNullOrWhiteSpace(email))
                                    sc.Item().Text("Email: " + email).FontColor("#444444");
                            });
                        });
                    });

                    col.Item().PaddingTop(12);

                    // Items table
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(24);   // No
                            c.RelativeColumn(1.1f); // Item Code
                            c.RelativeColumn(3f);   // Description
                            c.RelativeColumn(0.8f); // Unit
                            c.RelativeColumn(0.8f); // Qty
                            c.RelativeColumn(1.2f); // Unit Price
                            c.RelativeColumn(1.2f); // Total
                        });

                        table.Header(h =>
                        {
                            h.Cell().Background("#1E293B").Padding(5).AlignCenter()
                                .Text("No.").FontColor(Colors.White).Bold().FontSize(8.5f);
                            h.Cell().Background("#1E293B").Padding(5)
                                .Text("Item Code").FontColor(Colors.White).Bold().FontSize(8.5f);
                            h.Cell().Background("#1E293B").Padding(5)
                                .Text("Item Description").FontColor(Colors.White).Bold().FontSize(8.5f);
                            h.Cell().Background("#1E293B").Padding(5).AlignCenter()
                                .Text("Unit").FontColor(Colors.White).Bold().FontSize(8.5f);
                            h.Cell().Background("#1E293B").Padding(5).AlignCenter()
                                .Text("Qty").FontColor(Colors.White).Bold().FontSize(8.5f);
                            h.Cell().Background("#1E293B").Padding(5).AlignRight()
                                .Text("Unit Price (USD)").FontColor(Colors.White).Bold().FontSize(8.5f);
                            h.Cell().Background("#1E293B").Padding(5).AlignRight()
                                .Text("Total Price (USD)").FontColor(Colors.White).Bold().FontSize(8.5f);
                        });

                        int num = 0;
                        foreach (var item in order.Items)
                        {
                            num++;
                            var bg = num % 2 == 0 ? "#F8FAFC" : "#FFFFFF";
                            var code = (item.MedicineName.Length >= 3
                                ? item.MedicineName[..3].ToUpper()
                                : item.MedicineId.ToString("D3"))
                                + "-" + ((int)(item.UnitCost * 100) % 900 + 100);

                            table.Cell().Background(bg).BorderBottom(1).BorderColor("#E2E8F0")
                                .Padding(5).AlignCenter().Text(num.ToString()).FontSize(9);
                            table.Cell().Background(bg).BorderBottom(1).BorderColor("#E2E8F0")
                                .Padding(5).Text(code).FontSize(9).FontColor("#64748B");
                            table.Cell().Background(bg).BorderBottom(1).BorderColor("#E2E8F0")
                                .Padding(5).Text(item.MedicineName).FontSize(9);
                            table.Cell().Background(bg).BorderBottom(1).BorderColor("#E2E8F0")
                                .Padding(5).AlignCenter().Text(string.IsNullOrWhiteSpace(item.Unit) ? "Box" : item.Unit).FontSize(9);
                            table.Cell().Background(bg).BorderBottom(1).BorderColor("#E2E8F0")
                                .Padding(5).AlignCenter().Text(item.Quantity.ToString()).FontSize(9);
                            table.Cell().Background(bg).BorderBottom(1).BorderColor("#E2E8F0")
                                .Padding(5).AlignRight().Text(item.UnitCost.ToString("F2")).FontSize(9);
                            table.Cell().Background(bg).BorderBottom(1).BorderColor("#E2E8F0")
                                .Padding(5).AlignRight().Text(item.LineTotal.ToString("F2")).FontSize(9);
                        }
                    });

                    col.Item().PaddingTop(10);

                    // Terms + Totals
                    col.Item().Row(row =>
                    {
                        // Terms
                        row.RelativeItem().Column(tc =>
                        {
                            tc.Item().Text("Terms & Conditions:").Bold().FontSize(9);
                            tc.Item().PaddingTop(3)
                                .Text("1. Please supply the above items on or before the delivery date.")
                                .FontSize(8.5f).FontColor("#444444");
                            tc.Item().Text("2. Payment term: 30 days after delivery.")
                                .FontSize(8.5f).FontColor("#444444");
                            tc.Item().Text("3. All items must be of good quality and valid for at least 12 months.")
                                .FontSize(8.5f).FontColor("#444444");
                            if (!string.IsNullOrWhiteSpace(order.Notes))
                                tc.Item().PaddingTop(3)
                                    .Text("Note: " + order.Notes)
                                    .FontSize(8.5f).Italic().FontColor("#555555");
                        });

                        row.ConstantItem(14);

                        // Totals
                        row.ConstantItem(210).Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn();
                                c.RelativeColumn();
                            });

                            t.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(5)
                                .Text("Sub Total (USD)").FontSize(9);
                            t.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(5)
                                .AlignRight().Text($"{subTotal:F2}").FontSize(9);

                            t.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(5)
                                .Text("Discount (USD)").FontSize(9);
                            t.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(5)
                                .AlignRight().Text("0.00").FontSize(9);

                            t.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(5)
                                .Text("VAT 0% (USD)").FontSize(9);
                            t.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(5)
                                .AlignRight().Text("0.00").FontSize(9);

                            t.Cell().Background("#F1F5F9").Padding(5)
                                .Text("TOTAL (USD)").FontSize(9).Bold();
                            t.Cell().Background("#F1F5F9").Padding(5)
                                .AlignRight().Text($"{subTotal:F2}").FontSize(9).Bold();
                        });
                    });

                    col.Item().PaddingTop(20);

                    // Signatures
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Prepared By:").Bold().FontSize(9);
                            c.Item().PaddingTop(2).Text("Name:      " + printedBy).FontSize(9);
                            c.Item().Text("Position:  Store Manager").FontSize(9);
                            c.Item().PaddingTop(12).Text("Signature: ________________").FontSize(9);
                            c.Item().PaddingTop(2).Text("Date: " + orderDate.ToString("dd-MMM-yyyy")).FontSize(9);
                        });

                        row.ConstantItem(100).Column(c =>
                        {
                            c.Item().AlignCenter().Border(2).BorderColor("#C0392B")
                                .Padding(10).Text(pharmacyName).FontSize(7)
                                .Bold().FontColor("#C0392B");
                        });

                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Approved By:").Bold().FontSize(9);
                            c.Item().PaddingTop(2).Text("Name:      " + printedBy).FontSize(9);
                            c.Item().Text("Position:  Managing Director").FontSize(9);
                            c.Item().PaddingTop(12).Text("Signature: ________________").FontSize(9);
                            c.Item().PaddingTop(2).Text("Date: " + orderDate.ToString("dd-MMM-yyyy")).FontSize(9);
                        });
                    });
                });

                // ── FOOTER ────────────────────────────────────────────────
                page.Footer().PaddingTop(6).Row(row =>
                {
                    row.RelativeItem().Text("Thank you for your business!")
                        .Italic().FontSize(8).FontColor("#C0392B");
                    row.ConstantItem(80).AlignRight().Text(t =>
                    {
                        t.Span("Page ").FontSize(8).FontColor("#94A3B8");
                        t.CurrentPageNumber().FontSize(8).FontColor("#94A3B8");
                        t.Span(" of ").FontSize(8).FontColor("#94A3B8");
                        t.TotalPages().FontSize(8).FontColor("#94A3B8");
                    });
                });
            });
        });

        doc.GeneratePdf(path);
        return path;
    }
}
