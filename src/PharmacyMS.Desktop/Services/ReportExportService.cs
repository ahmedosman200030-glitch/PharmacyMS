using ClosedXML.Excel;
using PharmacyMS.Application.DTOs;
using PharmacyMS.Application.Interfaces.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PharmacyMS.Desktop.Services;

public static class ReportExportService
{
    public static void ExportExcel(
        string path,
        DateTime from, DateTime to,
        decimal revenue, decimal cost, decimal profit, int transactions,
        IEnumerable<TopSellingMedicine> topSellers,
        BrandingSettings? branding = null)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Report");

        var pharmacyName = GetPharmacyName(branding);
        ws.Cell(1, 1).Value = $"{pharmacyName} — Report: {from:yyyy-MM-dd} to {to:yyyy-MM-dd}";
        ws.Range(1, 1, 1, 4).Merge().Style.Font.Bold = true;

        var contactLine = GetContactLine(branding);
        if (!string.IsNullOrWhiteSpace(contactLine))
        {
            ws.Cell(2, 1).Value = contactLine;
            ws.Range(2, 1, 2, 4).Merge().Style.Font.FontColor = XLColor.FromHtml("#64748B");
            ws.Cell(2, 1).Style.Font.FontSize = 9;
        }

        ws.Cell(3, 1).Value = "Total Revenue";  ws.Cell(3, 2).Value = revenue;
        ws.Cell(4, 1).Value = "Purchase Cost";  ws.Cell(4, 2).Value = cost;
        ws.Cell(5, 1).Value = "Gross Profit";   ws.Cell(5, 2).Value = profit;
        ws.Cell(6, 1).Value = "Transactions";   ws.Cell(6, 2).Value = transactions;

        ws.Cell(8, 1).Value = "Medicine";
        ws.Cell(8, 2).Value = "Qty Sold";
        ws.Cell(8, 3).Value = "Revenue";
        var headerRange = ws.Range(8, 1, 8, 3);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#8B0000");
        headerRange.Style.Font.FontColor = XLColor.White;

        int row = 9;
        foreach (var item in topSellers)
        {
            ws.Cell(row, 1).Value = item.MedicineName;
            ws.Cell(row, 2).Value = item.QuantitySold;
            ws.Cell(row, 3).Value = item.Revenue;
            if (row % 2 == 0)
                ws.Range(row, 1, row, 3).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF5F5");
            row++;
        }

        ws.Columns().AdjustToContents();
        wb.SaveAs(path);
    }

    public static void ExportStockReconciliationExcel(
        string path,
        DateTime monthStart, DateTime monthEnd,
        IEnumerable<MonthlyStockReconciliationRow> rows,
        BrandingSettings? branding = null)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Stock Reconciliation");

        var pharmacyName = GetPharmacyName(branding);
        ws.Cell(1, 1).Value = $"{pharmacyName} — Monthly Stock Reconciliation: {monthStart:MMMM yyyy}";
        ws.Range(1, 1, 1, 6).Merge().Style.Font.Bold = true;

        var contactLine = GetContactLine(branding);
        if (!string.IsNullOrWhiteSpace(contactLine))
        {
            ws.Cell(2, 1).Value = contactLine;
            ws.Range(2, 1, 2, 6).Merge().Style.Font.FontColor = XLColor.FromHtml("#64748B");
            ws.Cell(2, 1).Style.Font.FontSize = 9;
        }

        ws.Cell(3, 1).Value = "Medicine";
        ws.Cell(3, 2).Value = "Opening Stock";
        ws.Cell(3, 3).Value = "Received";
        ws.Cell(3, 4).Value = "Dispensed";
        ws.Cell(3, 5).Value = "Adjustments";
        ws.Cell(3, 6).Value = "Closing Stock";
        var headerRange = ws.Range(3, 1, 3, 6);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#8B0000");
        headerRange.Style.Font.FontColor = XLColor.White;

        int row = 4;
        foreach (var r in rows)
        {
            ws.Cell(row, 1).Value = r.MedicineName;
            ws.Cell(row, 2).Value = r.OpeningStock;
            ws.Cell(row, 3).Value = r.Received;
            ws.Cell(row, 4).Value = r.Dispensed;
            ws.Cell(row, 5).Value = r.Adjustments;
            ws.Cell(row, 6).Value = r.ClosingStock;
            if (row % 2 == 0)
                ws.Range(row, 1, row, 6).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF5F5");
            row++;
        }

        ws.Columns().AdjustToContents();
        wb.SaveAs(path);
    }

    public static void ExportPdf(
        string path,
        DateTime from, DateTime to,
        decimal revenue, decimal cost, decimal profit, int transactions,
        IEnumerable<TopSellingMedicine> topSellers,
        BrandingSettings? branding = null)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var pharmacyName = GetPharmacyName(branding);
        var red    = "#C0122C";
        var navy   = "#8B0000";
        var gray   = "#64748B";
        var border = "#E2E8F0";

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                // ── HEADER ──────────────────────────────────────────────
                page.Header().Column(h =>
                {
                    h.Item().Row(row =>
                    {
                        // Left: pharmacy name + subtitle
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(pharmacyName)
                                .Bold().FontSize(18).FontColor(red);
                            c.Item().Text("Management System")
                                .FontSize(10).FontColor(gray);
                        });

                        // Right: PHARMACY REPORT title
                        row.ConstantItem(200).AlignRight().Column(c =>
                        {
                            c.Item().AlignRight().Text("PHARMACY REPORT")
                                .Bold().FontSize(16).FontColor("#0F172A");
                        });
                    });

                    h.Item().PaddingTop(6).LineHorizontal(1.5f).LineColor(red);

                    // Date range row
                    h.Item().PaddingTop(8).PaddingBottom(4).Row(row =>
                    {
                        row.AutoItem().Text("📅 ").FontSize(11);
                        row.RelativeItem().Text($"{from:yyyy-MM-dd}  to  {to:yyyy-MM-dd}")
                            .FontSize(11).FontColor("#334155");
                    });
                });

                // ── CONTENT ─────────────────────────────────────────────
                page.Content().PaddingTop(12).Column(col =>
                {
                    // Summary cards row
                    col.Item().Row(row =>
                    {
                        SummaryCard(row, "📈", $"${revenue:F2}", "Total Revenue",    "#22C55E");
                        SummaryCard(row, "🛒", $"${cost:F2}",    "Purchase Cost",    "#F97316");
                        SummaryCard(row, "$",  $"${profit:F2}",  "Gross Profit",     "#3B82F6");
                        SummaryCard(row, "⇄",  transactions.ToString(), "Transactions", "#8B5CF6");
                    });

                    // Section title
                    col.Item().PaddingTop(20).PaddingBottom(8).Row(row =>
                    {
                        row.RelativeItem().LineHorizontal(1).LineColor(red);
                        row.AutoItem().PaddingHorizontal(10)
                            .Text("TOP SELLING MEDICINES")
                            .Bold().FontSize(12).FontColor(red);
                        row.RelativeItem().LineHorizontal(1).LineColor(red);
                    });

                    // Table
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(4);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                        });

                        // Table header
                        foreach (var h in new[] { "Medicine", "Qty Sold", "Revenue" })
                        {
                            table.Cell().Background(navy).Padding(8)
                                .Text(h).Bold().FontColor("#FFFFFF").FontSize(10);
                        }

                        // Table rows
                        bool alt = false;
                        foreach (var item in topSellers)
                        {
                            var bg = alt ? "#FFF5F5" : "#FFFFFF";
                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(border)
                                .Padding(7).Text(item.MedicineName).FontSize(10);
                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(border)
                                .Padding(7).AlignCenter().Text(item.QuantitySold.ToString()).FontSize(10);
                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(border)
                                .Padding(7).AlignRight().Text($"${item.Revenue:F2}").FontSize(10);
                            alt = !alt;
                        }
                    });
                });

                // ── FOOTER ──────────────────────────────────────────────
                page.Footer().PaddingTop(8).Column(f =>
                {
                    f.Item().LineHorizontal(1).LineColor(red);
                    f.Item().PaddingTop(8).Row(row =>
                    {
                        // Left: address block
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(pharmacyName).Bold().FontSize(10);
                            foreach (var (icon, text) in GetFooterLines(branding))
                            {
                                c.Item().Text($"{icon}  {text}").FontSize(9).FontColor(gray);
                            }
                        });

                        // Divider
                        row.ConstantItem(1).Background(border);

                        // Right: generated timestamp
                        row.ConstantItem(160).PaddingLeft(12).Column(c =>
                        {
                            c.Item().Text("📅 Generated").FontSize(9).FontColor(gray);
                            c.Item().Text(DateTime.Now.ToString("yyyy-MM-dd HH:mm"))
                                .FontSize(10).Bold();
                        });
                    });
                });
            });
        }).GeneratePdf(path);
    }

    public static void ExportStockReconciliationPdf(
        string path,
        DateTime monthStart, DateTime monthEnd,
        IEnumerable<MonthlyStockReconciliationRow> rows,
        BrandingSettings? branding = null)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var pharmacyName = GetPharmacyName(branding);
        var red    = "#C0122C";
        var navy   = "#8B0000";
        var gray   = "#64748B";
        var border = "#E2E8F0";

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                // ── HEADER ──────────────────────────────────────────────
                page.Header().Column(h =>
                {
                    h.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(pharmacyName)
                                .Bold().FontSize(18).FontColor(red);
                            c.Item().Text("Management System")
                                .FontSize(10).FontColor(gray);
                        });

                        row.ConstantItem(220).AlignRight().Column(c =>
                        {
                            c.Item().AlignRight().Text("MONTHLY STOCK RECONCILIATION")
                                .Bold().FontSize(14).FontColor("#0F172A");
                        });
                    });

                    h.Item().PaddingTop(6).LineHorizontal(1.5f).LineColor(red);

                    h.Item().PaddingTop(8).PaddingBottom(4).Row(row =>
                    {
                        row.AutoItem().Text("📅 ").FontSize(11);
                        row.RelativeItem().Text(monthStart.ToString("MMMM yyyy"))
                            .FontSize(11).FontColor("#334155");
                    });
                });

                // ── CONTENT ─────────────────────────────────────────────
                page.Content().PaddingTop(12).Column(col =>
                {
                    col.Item().PaddingBottom(8).Row(row =>
                    {
                        row.RelativeItem().LineHorizontal(1).LineColor(red);
                        row.AutoItem().PaddingHorizontal(10)
                            .Text("STOCK MOVEMENT BY MEDICINE")
                            .Bold().FontSize(12).FontColor(red);
                        row.RelativeItem().LineHorizontal(1).LineColor(red);
                    });

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(4);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                        });

                        foreach (var h in new[] { "Medicine", "Opening", "Received", "Dispensed", "Adjustments", "Closing" })
                        {
                            table.Cell().Background(navy).Padding(8)
                                .Text(h).Bold().FontColor("#FFFFFF").FontSize(9);
                        }

                        bool alt = false;
                        foreach (var r in rows)
                        {
                            var bg = alt ? "#FFF5F5" : "#FFFFFF";
                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(border)
                                .Padding(7).Text(r.MedicineName).FontSize(9);
                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(border)
                                .Padding(7).AlignCenter().Text(r.OpeningStock.ToString()).FontSize(9);
                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(border)
                                .Padding(7).AlignCenter().Text(r.Received.ToString()).FontSize(9).FontColor("#22C55E");
                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(border)
                                .Padding(7).AlignCenter().Text(r.Dispensed.ToString()).FontSize(9).FontColor("#3B82F6");
                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(border)
                                .Padding(7).AlignCenter().Text(r.Adjustments.ToString()).FontSize(9).FontColor("#F97316");
                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(border)
                                .Padding(7).AlignCenter().Text(r.ClosingStock.ToString()).Bold().FontSize(9).FontColor("#0F172A");
                            alt = !alt;
                        }
                    });
                });

                // ── FOOTER ──────────────────────────────────────────────
                page.Footer().PaddingTop(8).Column(f =>
                {
                    f.Item().LineHorizontal(1).LineColor(red);
                    f.Item().PaddingTop(8).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(pharmacyName).Bold().FontSize(10);
                            foreach (var (icon, text) in GetFooterLines(branding))
                            {
                                c.Item().Text($"{icon}  {text}").FontSize(9).FontColor(gray);
                            }
                        });

                        row.ConstantItem(1).Background(border);

                        row.ConstantItem(160).PaddingLeft(12).Column(c =>
                        {
                            c.Item().Text("📅 Generated").FontSize(9).FontColor(gray);
                            c.Item().Text(DateTime.Now.ToString("yyyy-MM-dd HH:mm"))
                                .FontSize(10).Bold();
                        });
                    });
                });
            });
        }).GeneratePdf(path);
    }

    private static void SummaryCard(RowDescriptor row, string icon, string value, string label, string color)
    {
        row.RelativeItem().Padding(4).Border(1).BorderColor("#E2E8F0").CornerRadius(6).Padding(10).Column(c =>
        {
            c.Item().AlignCenter().Text(icon).FontSize(20).FontColor(color);
            c.Item().PaddingTop(4).AlignCenter().Text(value).Bold().FontSize(15).FontColor(color);
            c.Item().AlignCenter().Text(label).FontSize(9).FontColor("#64748B");
            c.Item().PaddingTop(4).LineHorizontal(2).LineColor(color);
        });
    }

    private static string GetPharmacyName(BrandingSettings? branding)
    {
        if (branding == null) return "PharmacyMS";
        if (!string.IsNullOrWhiteSpace(branding.PharmacyName)) return branding.PharmacyName!;
        return string.IsNullOrWhiteSpace(branding.AppName) ? "PharmacyMS" : branding.AppName;
    }

    private static string GetContactLine(BrandingSettings? branding)
    {
        if (branding == null) return "";
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(branding.Address)) parts.Add(branding.Address!);
        var phone = !string.IsNullOrWhiteSpace(branding.PhoneNumber) ? branding.PhoneNumber : branding.MobileNumber;
        if (!string.IsNullOrWhiteSpace(phone)) parts.Add(phone!);
        if (!string.IsNullOrWhiteSpace(branding.Email)) parts.Add(branding.Email!);
        return string.Join("   |   ", parts);
    }

    private static List<(string Icon, string Text)> GetFooterLines(BrandingSettings? branding)
    {
        var lines = new List<(string, string)>();
        if (branding == null) return lines;
        if (!string.IsNullOrWhiteSpace(branding.Address))
            lines.Add(("📍", branding.Address!));
        var phone = !string.IsNullOrWhiteSpace(branding.PhoneNumber) ? branding.PhoneNumber : branding.MobileNumber;
        if (!string.IsNullOrWhiteSpace(phone))
            lines.Add(("📞", phone!));
        if (!string.IsNullOrWhiteSpace(branding.Email))
            lines.Add(("✉", branding.Email!));
        return lines;
    }
}
