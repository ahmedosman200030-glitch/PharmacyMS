using Avalonia.Controls;
using PharmacyMS.Desktop.ViewModels;
using PharmacyMS.Domain.Entities;
using PharmacyMS.Application.Interfaces.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PharmacyMS.Desktop.Views.Sales;

public partial class SalesHistoryView : UserControl
{
    private readonly SalesHistoryViewModel _vm;
    private readonly IBrandingService? _brandingService;

    public SalesHistoryView() { InitializeComponent(); }
    public SalesHistoryView(SalesHistoryViewModel vm, IBrandingService? brandingService = null)
    {
        InitializeComponent();
        _vm = vm;
        _brandingService = brandingService;

        SalesGrid.ItemsSource = _vm.Sales;
        SalesGrid.LoadingRow += (_, e) => e.Row.Header = (e.Row.GetIndex() + 1).ToString();

        const string AllCustomersOption = "All Customers";

        AttachedToVisualTree += async (_, _) =>
        {
            await _vm.LoadAllAsync();
            PopulateCustomerFilter(AllCustomersOption);
        };

        PrintStatementsButton.Click += async (_, _) =>
        {
            var selected = CustomerFilterCombo.SelectedItem as string ?? AllCustomersOption;
            var sales = selected == AllCustomersOption
                ? _vm.Sales.ToList()
                : _vm.Sales.Where(s => s.CustomerName == selected).ToList();

            if (sales.Count == 0)
            {
                return;
            }

            await PrintStatementsAsync(sales);
        };

        SalesGrid.AddHandler(Button.ClickEvent, async (object? sender, Avalonia.Interactivity.RoutedEventArgs e) =>
        {
            if (e.Source is not Button btn) return;
            if (btn.DataContext is not Sale sale) return;
            if (btn.Name == "ReprintBtn") await ReprintAsync(sale);
            else if (btn.Name == "StatementBtn") await PrintStatementsAsync(new List<Sale> { sale });
        }, Avalonia.Interactivity.RoutingStrategies.Bubble);
    }

    private void PopulateCustomerFilter(string allOption)
    {
        var customers = _vm.Sales.Select(s => s.CustomerName).Distinct().OrderBy(n => n).ToList();
        var items = new List<string> { allOption };
        items.AddRange(customers);
        CustomerFilterCombo.ItemsSource = items;
        CustomerFilterCombo.SelectedIndex = 0;
    }

    private async Task PrintStatementsAsync(List<Sale> sales)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var brandingService = _brandingService ?? Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions
            .GetRequiredService<IBrandingService>(PharmacyMS.Desktop.Program.Services);
        var branding = await brandingService.GetAsync();

        var pharmacyName = !string.IsNullOrWhiteSpace(branding.PharmacyName) ? branding.PharmacyName : branding.AppName;
        var address = branding.Address ?? "";
        var phone = branding.PhoneNumber ?? "";
        var email = branding.Email ?? "";
        var contactNumber = branding.ContactNumber ?? "";
        var logoPath = branding.LogoPath ?? "";
        var printedBy = PharmacyMS.Application.Services.SessionManager.CurrentUser?.FullName ?? "Admin";

        // Pre-fetch full customer details (phone/email/address) before composing the PDF
        var customerDetails = new Dictionary<int, (string Phone, string Email, string Address)>();
        foreach (var sale in sales)
        {
            if (sale.CustomerId.HasValue && !customerDetails.ContainsKey(sale.CustomerId.Value))
            {
                var customer = await _vm.GetCustomerAsync(sale.CustomerId.Value);
                customerDetails[sale.CustomerId.Value] = (
                    customer?.Phone ?? "",
                    customer?.Email ?? "",
                    customer?.Address ?? "");
            }
        }

        var doc = Document.Create(container =>
        {
            foreach (var sale in sales)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.ConstantItem(220).Row(lr =>
                            {
                                if (!string.IsNullOrWhiteSpace(logoPath) && File.Exists(logoPath))
                                { lr.ConstantItem(50).Image(logoPath).FitArea(); lr.ConstantItem(6); }
                                lr.RelativeItem().Column(c =>
                                {
                                    c.Item().Text(pharmacyName).FontSize(14).Bold().FontColor("#0F2A43");
                                    c.Item().Text("Pharmacy Management System").FontSize(8).FontColor("#64748B");
                                    if (!string.IsNullOrWhiteSpace(address)) c.Item().Text($"📍 {address}").FontSize(8).FontColor("#64748B");
                                    if (!string.IsNullOrWhiteSpace(phone)) c.Item().Text($"📞 {phone}").FontSize(8).FontColor("#64748B");
                                    if (!string.IsNullOrWhiteSpace(email)) c.Item().Text($"✉ {email}").FontSize(8).FontColor("#64748B");
                                    if (!string.IsNullOrWhiteSpace(contactNumber)) c.Item().Text($"☎ Contact: {contactNumber}").FontSize(8).Bold().FontColor("#0F172A");
                                });
                            });

                            row.RelativeItem().AlignCenter().Column(c =>
                            {
                                c.Item().AlignCenter().Text("SALES STATEMENT").FontSize(20).Bold().FontColor("#0F172A");
                                c.Item().AlignCenter().Text($"Invoice: {sale.InvoiceNumber}").FontSize(11).FontColor("#64748B");
                                c.Item().AlignCenter().PaddingTop(4).Text($"Date: {sale.CreatedAt:dd MMM yyyy  HH:mm}").FontSize(10).FontColor("#3B82F6").Bold();
                            });

                            row.ConstantItem(160).Border(1).BorderColor("#E2E8F0").Padding(8).Column(c =>
                            {
                                c.Item().Row(r => { r.ConstantItem(60).Text("Printed By").FontSize(8).FontColor("#64748B"); r.RelativeItem().Text($": {printedBy}").FontSize(8); });
                                c.Item().Row(r => { r.ConstantItem(60).Text("Date").FontSize(8).FontColor("#64748B"); r.RelativeItem().Text($": {DateTime.Today:dd/MM/yyyy}").FontSize(8); });
                                c.Item().Row(r => { r.ConstantItem(60).Text("Time").FontSize(8).FontColor("#64748B"); r.RelativeItem().Text($": {DateTime.Now:hh:mm tt}").FontSize(8); });
                            });
                        });

                        col.Item().PaddingVertical(6).LineHorizontal(2).LineColor("#3B82F6");

                        col.Item().Background("#F8FAFC").Border(1).BorderColor("#E2E8F0").Padding(10).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("CUSTOMER DETAILS").FontSize(9).Bold().FontColor("#64748B");
                                c.Item().PaddingTop(4).Text(sale.CustomerName).FontSize(13).Bold();
                                if (sale.CustomerId.HasValue && customerDetails.TryGetValue(sale.CustomerId.Value, out var cd))
                                {
                                    if (!string.IsNullOrWhiteSpace(cd.Phone)) c.Item().Text($"📞 {cd.Phone}").FontSize(9).FontColor("#64748B");
                                    if (!string.IsNullOrWhiteSpace(cd.Email)) c.Item().Text($"✉ {cd.Email}").FontSize(9).FontColor("#64748B");
                                    if (!string.IsNullOrWhiteSpace(cd.Address)) c.Item().Text($"📍 {cd.Address}").FontSize(9).FontColor("#64748B");
                                }
                            });
                            row.ConstantItem(220).Column(c =>
                            {
                                c.Item().Text("PAYMENT DETAILS").FontSize(9).Bold().FontColor("#64748B");
                                c.Item().PaddingTop(4).Row(r => { r.ConstantItem(80).Text("Method").FontSize(9).FontColor("#64748B"); r.RelativeItem().Text($": {sale.PaymentMethod}").FontSize(9).Bold(); });
                                c.Item().Row(r => { r.ConstantItem(80).Text("Amount Paid").FontSize(9).FontColor("#64748B"); r.RelativeItem().Text($": ${sale.AmountPaid:F2}").FontSize(9); });
                                if (sale.ChangeDue > 0)
                                    c.Item().Row(r => { r.ConstantItem(80).Text("Change Due").FontSize(9).FontColor("#64748B"); r.RelativeItem().Text($": ${sale.ChangeDue:F2}").FontSize(9); });
                            });
                        });
                    });

                    page.Content().PaddingTop(14).Column(col =>
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(4);
                                c.RelativeColumn(1);
                                c.RelativeColumn(1.5f);
                                c.RelativeColumn(1.5f);
                            });

                            table.Header(h =>
                            {
                                h.Cell().Text("Item").FontSize(9).Bold().FontColor("#64748B");
                                h.Cell().AlignCenter().Text("Qty").FontSize(9).Bold().FontColor("#64748B");
                                h.Cell().AlignRight().Text("Unit Price").FontSize(9).Bold().FontColor("#64748B");
                                h.Cell().AlignRight().Text("Total").FontSize(9).Bold().FontColor("#64748B");
                                h.Cell().ColumnSpan(4).PaddingTop(4).BorderBottom(1).BorderColor("#E2E8F0");
                            });

                            foreach (var item in sale.Items)
                            {
                                table.Cell().PaddingVertical(3).Text(item.MedicineName).FontSize(9.5f);
                                table.Cell().PaddingVertical(3).AlignCenter().Text(item.Quantity.ToString()).FontSize(9.5f);
                                table.Cell().PaddingVertical(3).AlignRight().Text($"${item.UnitPrice:F2}").FontSize(9.5f);
                                table.Cell().PaddingVertical(3).AlignRight().Text($"${item.UnitPrice * item.Quantity:F2}").FontSize(9.5f).Bold();
                            }
                        });

                        col.Item().PaddingTop(14).AlignRight().Width(260).Column(totals =>
                        {
                            totals.Item().Row(r => { r.RelativeItem().Text("Subtotal").FontColor("#64748B"); r.RelativeItem().AlignRight().Text($"${sale.Subtotal:F2}"); });
                            if (sale.TotalDiscount > 0)
                                totals.Item().Row(r => { r.RelativeItem().Text("Discount").FontColor("#DC2626"); r.RelativeItem().AlignRight().Text($"-${sale.TotalDiscount:F2}").FontColor("#DC2626"); });
                            totals.Item().Row(r => { r.RelativeItem().Text($"Tax ({sale.TaxRate * 100:0.##}%)").FontColor("#64748B"); r.RelativeItem().AlignRight().Text($"${sale.TaxAmount:F2}"); });
                            totals.Item().PaddingVertical(4).LineHorizontal(1).LineColor("#0F172A");
                            totals.Item().Row(r => { r.RelativeItem().Text("TOTAL").FontSize(13).Bold(); r.RelativeItem().AlignRight().Text($"${sale.TotalAmount:F2}").FontSize(13).Bold(); });
                        });
                    });

                    page.Footer().PaddingTop(10).AlignCenter().Text("This is a system-generated sales statement.")
                        .FontSize(8).FontColor("#94A3B8");
                });
            }
        });

        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PharmaPro", "Statements");
        Directory.CreateDirectory(folder);
        var fileName = sales.Count == 1
            ? $"Statement-{sales[0].InvoiceNumber}.pdf"
            : $"Statements-{DateTime.Now:yyyyMMdd-HHmmss}.pdf";
        var path = Path.Combine(folder, fileName);
        doc.GeneratePdf(path);

        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true }); }
        catch { /* ignore if no default PDF viewer */ }
    }

    private async Task ReprintAsync(Sale sale)
    {
        var receiptService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions
            .GetRequiredService<PharmacyMS.Application.Interfaces.Services.IReceiptService>(PharmacyMS.Desktop.Program.Services);

        var receipt = await receiptService.BuildReceiptAsync(
            sale, sale.CustomerName, sale.PaymentMethod, sale.AmountPaid, sale.ChangeDue, sale.TotalDiscount);

        var win = new ReceiptWindow(receipt, receiptService);
        win.Show();
    }
}
