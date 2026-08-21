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
        IEnumerable<DailySalesSummaryRow> dailySales,
        IEnumerable<PurchaseVsSalesRow> purchaseVsSales,
        IEnumerable<PaymentMethodBreakdownRow> payments,
        IEnumerable<TaxReportRow> taxReport,
        IEnumerable<InventoryValuationRow> inventory,
        IEnumerable<SupplierPaymentRow> suppliers,
        IEnumerable<TopSellingMedicine> topSellers,
        BrandingSettings? branding = null)
    {
        using var wb = new XLWorkbook();
        var pharmacyName = GetPharmacyName(branding);

        // Summary sheet
        var summary = wb.Worksheets.Add("Summary");
        summary.Cell(1, 1).Value = $"{pharmacyName} — Report: {from:yyyy-MM-dd} to {to:yyyy-MM-dd}";
        summary.Range(1, 1, 1, 4).Merge().Style.Font.Bold = true;

        var contactLine = GetContactLine(branding);
        if (!string.IsNullOrWhiteSpace(contactLine))
        {
            summary.Cell(2, 1).Value = contactLine;
            summary.Range(2, 1, 2, 4).Merge().Style.Font.FontColor = XLColor.FromHtml("#64748B");
            summary.Cell(2, 1).Style.Font.FontSize = 9;
        }

        summary.Cell(3, 1).Value = "Total Revenue";  summary.Cell(3, 2).Value = revenue;
        summary.Cell(4, 1).Value = "Purchase Cost";  summary.Cell(4, 2).Value = cost;
        summary.Cell(5, 1).Value = "Gross Profit";   summary.Cell(5, 2).Value = profit;
        summary.Cell(6, 1).Value = "Transactions";   summary.Cell(6, 2).Value = transactions;
        summary.Columns().AdjustToContents();

        WriteSheet(wb, "Daily Sales", new[] { "Date", "Transactions", "Revenue", "Discount", "Tax", "Net Revenue" },
            dailySales, r => new object[] { r.Date, r.Transactions, r.Revenue, r.Discount, r.Tax, r.NetRevenue });

        WriteSheet(wb, "Profit and Loss", new[] { "Medicine", "Qty Purchased", "Qty Sold", "Purchase Cost", "Sale Revenue", "Profit" },
            purchaseVsSales, r => new object[] { r.MedicineName, r.Purchased, r.Sold, r.PurchaseCost, r.SaleRevenue, r.Profit });

        WriteSheet(wb, "Payments", new[] { "Payment Method", "Transactions", "Total Amount" },
            payments, r => new object[] { r.Method, r.Count, r.Total });

        WriteSheet(wb, "Tax Report", new[] { "Date", "Revenue", "Tax Rate %", "Tax Collected" },
            taxReport, r => new object[] { r.Date, r.Revenue, r.TaxRate, r.TaxAmount });

        WriteSheet(wb, "Inventory Valuation", new[] { "Medicine", "Category", "Qty", "Cost Price", "Retail Price", "Cost Value", "Retail Value", "Potential Profit" },
            inventory, r => new object[] { r.MedicineName, r.Category, r.Quantity, r.CostPrice, r.RetailPrice, r.CostValue, r.RetailValue, r.PotentialProfit });

        WriteSheet(wb, "Supplier Payments", new[] { "Supplier", "Phone", "Invoices", "Total Amount", "Amount Paid", "Balance", "Status" },
            suppliers, r => new object[] { r.SupplierName, r.Phone, r.TotalInvoices, r.TotalAmount, r.AmountPaid, r.Balance, r.Status });

        WriteSheet(wb, "Top Sellers", new[] { "Medicine", "Qty Sold", "Revenue" },
            topSellers, r => new object[] { r.MedicineName, r.QuantitySold, r.Revenue });

        wb.SaveAs(path);
    }

    private static void WriteSheet<T>(XLWorkbook wb, string sheetName, string[] headers, IEnumerable<T> rows, Func<T, object[]> rowSelector)
    {
        var ws = wb.Worksheets.Add(sheetName);
        for (int i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        var headerRange = ws.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#8B0000");
        headerRange.Style.Font.FontColor = XLColor.White;

        int row = 2;
        foreach (var item in rows)
        {
            var vals = rowSelector(item);
            for (int c = 0; c < vals.Length; c++)
            {
                var v = vals[c];
                if (v is double d) ws.Cell(row, c + 1).Value = d;
                else if (v is int ii) ws.Cell(row, c + 1).Value = ii;
                else ws.Cell(row, c + 1).Value = v?.ToString() ?? "";
            }
            if (row % 2 == 0)
                ws.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF5F5");
            row++;
        }
        ws.Columns().AdjustToContents();
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
        IEnumerable<DailySalesSummaryRow> dailySales,
        IEnumerable<PurchaseVsSalesRow> purchaseVsSales,
        IEnumerable<PaymentMethodBreakdownRow> payments,
        IEnumerable<TaxReportRow> taxReport,
        IEnumerable<InventoryValuationRow> inventory,
        IEnumerable<SupplierPaymentRow> suppliers,
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

                page.Header().Column(h =>
                {
                    h.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(pharmacyName).Bold().FontSize(18).FontColor(red);
                            c.Item().Text("Management System").FontSize(10).FontColor(gray);
                        });

                        row.ConstantItem(200).AlignRight().Column(c =>
                        {
                            c.Item().AlignRight().Text("PHARMACY REPORT").Bold().FontSize(16).FontColor("#0F172A");
                        });
                    });

                    h.Item().PaddingTop(6).LineHorizontal(1.5f).LineColor(red);

                    h.Item().PaddingTop(8).PaddingBottom(4).Row(row =>
                    {
                        row.AutoItem().Text("📅 ").FontSize(11);
                        row.RelativeItem().Text($"{from:yyyy-MM-dd}  to  {to:yyyy-MM-dd}").FontSize(11).FontColor("#334155");
                    });
                });

                page.Content().PaddingTop(12).Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        SummaryCard(row, "📈", $"${revenue:F2}", "Total Revenue", "#22C55E");
                        SummaryCard(row, "🛒", $"${cost:F2}", "Purchase Cost", "#F97316");
                        SummaryCard(row, "$", $"${profit:F2}", "Gross Profit", "#3B82F6");
                        SummaryCard(row, "⇄", transactions.ToString(), "Transactions", "#8B5CF6");
                    });

                    AddSection(col, "DAILY SALES SUMMARY", red, navy, border,
                        new[] { "Date", "Transactions", "Revenue", "Discount", "Tax", "Net Revenue" },
                        dailySales,
                        r => new[] { r.Date, r.Transactions.ToString(), $"{r.Revenue:F2}", $"{r.Discount:F2}", $"{r.Tax:F2}", $"{r.NetRevenue:F2}" },
                        new[] { 3, 2, 2, 2, 2, 2 });

                    AddSection(col, "PROFIT & LOSS BY MEDICINE", red, navy, border,
                        new[] { "Medicine", "Purchased", "Sold", "Purchase Cost", "Sale Revenue", "Profit" },
                        purchaseVsSales,
                        r => new[] { r.MedicineName, $"{r.Purchased:F0}", $"{r.Sold:F0}", $"{r.PurchaseCost:F2}", $"{r.SaleRevenue:F2}", $"{r.Profit:F2}" },
                        new[] { 4, 2, 2, 2, 2, 2 });

                    AddSection(col, "PAYMENT METHOD BREAKDOWN", red, navy, border,
                        new[] { "Payment Method", "Transactions", "Total Amount" },
                        payments,
                        r => new[] { r.Method, r.Count.ToString(), $"{r.Total:F2}" },
                        new[] { 4, 2, 2 });

                    AddSection(col, "TAX REPORT", red, navy, border,
                        new[] { "Date", "Revenue", "Tax Rate %", "Tax Collected" },
                        taxReport,
                        r => new[] { r.Date, $"{r.Revenue:F2}", $"{r.TaxRate:F2}", $"{r.TaxAmount:F2}" },
                        new[] { 3, 2, 2, 2 });

                    AddSection(col, "INVENTORY VALUATION", red, navy, border,
                        new[] { "Medicine", "Category", "Qty", "Cost Price", "Retail Price", "Cost Value", "Retail Value", "Profit" },
                        inventory,
                        r => new[] { r.MedicineName, r.Category, r.Quantity.ToString(), $"{r.CostPrice:F2}", $"{r.RetailPrice:F2}", $"{r.CostValue:F2}", $"{r.RetailValue:F2}", $"{r.PotentialProfit:F2}" },
                        new[] { 3, 2, 1, 2, 2, 2, 2, 2 });

                    AddSection(col, "SUPPLIER PAYMENTS", red, navy, border,
                        new[] { "Supplier", "Phone", "Invoices", "Total Amount", "Amount Paid", "Balance", "Status" },
                        suppliers,
                        r => new[] { r.SupplierName, r.Phone, r.TotalInvoices.ToString(), $"{r.TotalAmount:F2}", $"{r.AmountPaid:F2}", $"{r.Balance:F2}", r.Status },
                        new[] { 3, 2, 1, 2, 2, 2, 2 });

                    AddSection(col, "TOP SELLING MEDICINES", red, navy, border,
                        new[] { "Medicine", "Qty Sold", "Revenue" },
                        topSellers,
                        r => new[] { r.MedicineName, r.QuantitySold.ToString(), $"{r.Revenue:F2}" },
                        new[] { 4, 2, 2 });
                });

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
                            c.Item().Text(DateTime.Now.ToString("yyyy-MM-dd HH:mm")).FontSize(10).Bold();
                        });
                    });
                });
            });
        }).GeneratePdf(path);
    }

    private static void AddSection<T>(
        QuestPDF.Fluent.ColumnDescriptor col, string title, string red, string navy, string border,
        string[] headers, IEnumerable<T> rows, Func<T, string[]> cellsSelector, int[] relativeWidths)
    {
        col.Item().PaddingTop(20).PaddingBottom(8).Row(row =>
        {
            row.RelativeItem().LineHorizontal(1).LineColor(red);
            row.AutoItem().PaddingHorizontal(10).Text(title).Bold().FontSize(12).FontColor(red);
            row.RelativeItem().LineHorizontal(1).LineColor(red);
        });

        col.Item().Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                foreach (var w in relativeWidths) c.RelativeColumn(w);
            });

            foreach (var h in headers)
                table.Cell().Background(navy).Padding(6).Text(h).Bold().FontColor("#FFFFFF").FontSize(9);

            bool alt = false;
            foreach (var item in rows)
            {
                var cells = cellsSelector(item);
                var bg = alt ? "#FFF5F5" : "#FFFFFF";
                foreach (var val in cells)
                {
                    table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(border).Padding(6).Text(val).FontSize(9);
                }
                alt = !alt;
            }
        });
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

                page.Header().Column(h =>
                {
                    h.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(pharmacyName).Bold().FontSize(18).FontColor(red);
                            c.Item().Text("Management System").FontSize(10).FontColor(gray);
                        });

                        row.ConstantItem(220).AlignRight().Column(c =>
                        {
                            c.Item().AlignRight().Text("MONTHLY STOCK RECONCILIATION").Bold().FontSize(14).FontColor("#0F172A");
                        });
                    });

                    h.Item().PaddingTop(6).LineHorizontal(1.5f).LineColor(red);

                    h.Item().PaddingTop(8).PaddingBottom(4).Row(row =>
                    {
                        row.AutoItem().Text("📅 ").FontSize(11);
                        row.RelativeItem().Text(monthStart.ToString("MMMM yyyy")).FontSize(11).FontColor("#334155");
                    });
                });

                page.Content().PaddingTop(12).Column(col =>
                {
                    col.Item().PaddingBottom(8).Row(row =>
                    {
                        row.RelativeItem().LineHorizontal(1).LineColor(red);
                        row.AutoItem().PaddingHorizontal(10).Text("STOCK MOVEMENT BY MEDICINE").Bold().FontSize(12).FontColor(red);
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
                            table.Cell().Background(navy).Padding(8).Text(h).Bold().FontColor("#FFFFFF").FontSize(9);
                        }

                        bool alt = false;
                        foreach (var r in rows)
                        {
                            var bg = alt ? "#FFF5F5" : "#FFFFFF";
                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(border).Padding(7).Text(r.MedicineName).FontSize(9);
                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(border).Padding(7).AlignCenter().Text(r.OpeningStock.ToString()).FontSize(9);
                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(border).Padding(7).AlignCenter().Text(r.Received.ToString()).FontSize(9).FontColor("#22C55E");
                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(border).Padding(7).AlignCenter().Text(r.Dispensed.ToString()).FontSize(9).FontColor("#3B82F6");
                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(border).Padding(7).AlignCenter().Text(r.Adjustments.ToString()).FontSize(9).FontColor("#F97316");
                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(border).Padding(7).AlignCenter().Text(r.ClosingStock.ToString()).Bold().FontSize(9).FontColor("#0F172A");
                            alt = !alt;
                        }
                    });
                });

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
                            c.Item().Text(DateTime.Now.ToString("yyyy-MM-dd HH:mm")).FontSize(10).Bold();
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
        if (branding == null) return "PharmaPro";
        if (!string.IsNullOrWhiteSpace(branding.PharmacyName)) return branding.PharmacyName!;
        return string.IsNullOrWhiteSpace(branding.AppName) ? "PharmaPro" : branding.AppName;
    }

    private static string GetContactLine(BrandingSettings? branding)
    {
        if (branding == null) return "";
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(branding.Address)) parts.Add(branding.Address!);
        var phone = !string.IsNullOrWhiteSpace(branding.PhoneNumber) ? branding.PhoneNumber : branding.MobileNumber;
        if (!string.IsNullOrWhiteSpace(phone)) parts.Add(phone!);
        if (!string.IsNullOrWhiteSpace(branding.Email)) parts.Add(branding.Email!);
        if (!string.IsNullOrWhiteSpace(branding.ContactNumber)) parts.Add($"Contact: {branding.ContactNumber}");
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
        if (!string.IsNullOrWhiteSpace(branding.ContactNumber))
            lines.Add(("☎", $"Contact: {branding.ContactNumber}"));
        return lines;
    }
}
