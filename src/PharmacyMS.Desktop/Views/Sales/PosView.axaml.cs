using Avalonia.Controls;
using PharmacyMS.Desktop.ViewModels;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Desktop.Views.Sales;

public partial class PosView : UserControl
{
    private readonly PosViewModel _vm;
    private string? _selectedCategory = "All";

    public PosView() { InitializeComponent(); }
    public PosView(PosViewModel vm)
    {
        InitializeComponent();
        _vm = vm;

        MedicineGrid.ItemsSource = _vm.AvailableMedicines;
        CartGrid.ItemsSource = _vm.Cart;
        CustomerCombo.ItemsSource = _vm.Customers;
        CustomerCombo.DisplayMemberBinding = new Avalonia.Data.Binding("Name");

        AttachedToVisualTree += async (_, _) =>
        {
            await _vm.LoadAsync();
            BuildCategoryButtons();
            if (CustomerCombo.ItemsSource is System.Collections.ObjectModel.ObservableCollection<Customer> c && c.Count > 0)
                CustomerCombo.SelectedIndex = 0;
            RefreshTotals();
        };

        SearchBox.PropertyChanged += (_, e) =>
        {
            if (e.Property == TextBox.TextProperty) RunFilter();
        };

        AddToCartButton.Click += (_, _) =>
        {
            if (MedicineGrid.SelectedItem is Medicine m && int.TryParse(QtyBox.Text, out var qty) && qty > 0)
            {
                if (!_vm.AddToCart(m, qty))
                    ShowStatus("Not enough stock for that quantity.");
                else
                    ShowStatus("");
                RefreshTotals();
            }
            else
            {
                ShowStatus("Select a medicine first.");
            }
        };

        RemoveItemButton.Click += (_, _) =>
        {
            if (CartGrid.SelectedItem is CartLine line)
            {
                _vm.RemoveFromCart(line);
                RefreshTotals();
            }
        };

        CartGrid.AddHandler(Button.ClickEvent, (object? sender, Avalonia.Interactivity.RoutedEventArgs e) =>
        {
            if (e.Source is not Button btn) return;
            if (btn.DataContext is not CartLine line) return;
            switch (btn.Name)
            {
                case "IncBtn": _vm.IncreaseQty(line); break;
                case "DecBtn": _vm.DecreaseQty(line); break;
                case "DiscUpBtn": _vm.IncreaseDiscount(line); break;
                case "DiscDownBtn": _vm.DecreaseDiscount(line); break;
                case "RemoveBtn": _vm.RemoveFromCart(line); break;
            }
            RefreshTotals();
            // Force grid to refresh — ObservableCollection won't fire for property changes on existing items
            var items = _vm.Cart.ToList();
            CartGrid.ItemsSource = null;
            CartGrid.ItemsSource = items;
            _vm.Cart.Clear();
            foreach (var i in items) _vm.Cart.Add(i);
        }, Avalonia.Interactivity.RoutingStrategies.Bubble);

        AmountReceivedBox.PropertyChanged += (_, e) =>
        {
            if (e.Property == TextBox.TextProperty)
            {
                if (decimal.TryParse(AmountReceivedBox.Text, out var a)) _vm.AmountReceived = a;
                RefreshTotals();
            }
        };

        PrescriptionCheck.IsCheckedChanged += (_, _) => _vm.IsPrescription = PrescriptionCheck.IsChecked == true;

        CreditSaleCheck.IsCheckedChanged += (_, _) =>
        {
            _vm.IsCreditSale = CreditSaleCheck.IsChecked == true;
            CreditCustomerNameBox.IsVisible = _vm.IsCreditSale;
        };
        CreditCustomerNameBox.PropertyChanged += (_, e) =>
        {
            if (e.Property == TextBox.TextProperty) _vm.CreditCustomerName = CreditCustomerNameBox.Text ?? "";
        };

        NotesButton.Click += (_, _) => NotesBox.IsVisible = !NotesBox.IsVisible;
        NotesBox.PropertyChanged += (_, e) =>
        {
            if (e.Property == TextBox.TextProperty) _vm.Notes = NotesBox.Text ?? "";
        };

        PayCash.IsCheckedChanged += (_, _) => { if (PayCash.IsChecked == true) _vm.PaymentMethod = "Cash"; };
        PayCard.IsCheckedChanged += (_, _) => { if (PayCard.IsChecked == true) _vm.PaymentMethod = "Card"; };
        PayMobile.IsCheckedChanged += (_, _) => { if (PayMobile.IsChecked == true) _vm.PaymentMethod = "Mobile Money"; };
        PayInsurance.IsCheckedChanged += (_, _) => { if (PayInsurance.IsChecked == true) _vm.PaymentMethod = "Insurance"; };

        ClearCartButton.Click += (_, _) =>
        {
            _vm.ClearCart();
            AmountReceivedBox.Text = "0";
            PrescriptionCheck.IsChecked = false;
            NotesBox.Text = "";
            NotesBox.IsVisible = false;
            PayCash.IsChecked = true;
            RefreshTotals();
        };

        HoldSaleButton.Click += (_, _) =>
        {
            var label = $"Hold {DateTime.Now:HH:mm:ss}";
            _vm.HoldCurrentSale(label);
            AmountReceivedBox.Text = "0";
            RefreshTotals();
            ShowStatus($"Sale held as \"{label}\".");
        };

        RecallSaleButton.Click += (_, _) =>
        {
            if (_vm.HeldSales.Count == 0) { ShowStatus("No held sales."); return; }
            _vm.RecallHeldSale(_vm.HeldSales.Last());
            RefreshTotals();
            ShowStatus("");
        };

        CalculatorButton.Click += (_, _) => ShowStatus("Calculator not wired up yet.");
        PrintReceiptButton.Click += async (_, _) =>
        {
            if (_vm.Cart.Count == 0) { ShowStatus("Cart is empty — complete or add items first."); return; }
            // Build a preview receipt from current cart (before checkout)
            var receiptService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions
                .GetRequiredService<PharmacyMS.Application.Interfaces.Services.IReceiptService>(PharmacyMS.Desktop.Program.Services);
            var brandingService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions
                .GetRequiredService<PharmacyMS.Application.Interfaces.Services.IBrandingService>(PharmacyMS.Desktop.Program.Services);
            var branding = await brandingService.GetAsync();

            var customerName = (CustomerCombo.SelectedItem as PharmacyMS.Domain.Entities.Customer)?.Name ?? "Walk-in Customer";
            var paymentMethod = _vm.PaymentMethod;

            var previewSale = new PharmacyMS.Domain.Entities.Sale
            {
                InvoiceNumber = "PREVIEW-" + DateTime.Now.ToString("HHmmss"),
                CreatedAt = DateTime.Now,
                CashierId = PharmacyMS.Application.Services.SessionManager.CurrentUser?.Id ?? 0,
                Subtotal = _vm.DiscountedSubtotal,
                TaxRate = _vm.TaxRate,
                TaxAmount = _vm.TaxAmount,
                TotalAmount = _vm.Total,
                Items = _vm.Cart.Select(c => new PharmacyMS.Domain.Entities.SaleItem
                {
                    MedicineId = c.MedicineId,
                    MedicineName = c.Name,
                    UnitPrice = c.UnitPrice,
                    Quantity = c.Quantity
                }).ToList()
            };

            var receipt = await receiptService.BuildReceiptAsync(
                previewSale, customerName, paymentMethod, _vm.AmountReceived, _vm.ChangeDue, _vm.TotalDiscount);

            var win = new ReceiptWindow(receipt, receiptService);
            win.Show();
        };

        CompleteSaleButton.Click += async (_, _) =>
        {
            if (_vm.Cart.Count == 0) { ShowStatus("Cart is empty."); return; }

            if (_vm.IsCreditSale)
            {
                var selected = CustomerCombo.SelectedItem as PharmacyMS.Domain.Entities.Customer;
                var hasRealCustomer = selected != null && selected.Id != 0;
                var hasTypedName = !string.IsNullOrWhiteSpace(CreditCustomerNameBox.Text);
                if (!hasRealCustomer && !hasTypedName)
                {
                    ShowStatus("Credit sale requires a customer — select one or type a name.");
                    return;
                }

                var existingBalance = hasRealCustomer
                    ? await _vm.GetOutstandingBalanceAsync(selected!.Id)
                    : 0m;

                if (existingBalance > 0)
                {
                    var owner = TopLevel.GetTopLevel(this) as Window;
                    var confirmed = owner != null && await PharmacyMS.Desktop.Views.Shared.ConfirmDialog.ShowAsync(
                        owner,
                        "Existing Balance",
                        $"This customer already owes ${existingBalance:F2}. Continue with this credit sale?");
                    if (!confirmed)
                    {
                        ShowStatus("Sale cancelled.");
                        return;
                    }
                }
            }

            var receiptService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions
                .GetRequiredService<PharmacyMS.Application.Interfaces.Services.IReceiptService>(PharmacyMS.Desktop.Program.Services);
            var customerName = (CustomerCombo.SelectedItem as PharmacyMS.Domain.Entities.Customer)?.Name
                ?? (_vm.IsCreditSale ? _vm.CreditCustomerName : "Walk-in Customer");
            var paymentMethod = _vm.PaymentMethod;
            var amountReceived = _vm.AmountReceived;
            var changeDue = _vm.ChangeDue;
            var totalDiscount = _vm.TotalDiscount;

            var sale = await _vm.CheckoutAsync();

            AmountReceivedBox.Text = "0";
            PrescriptionCheck.IsChecked = false;
            NotesBox.Text = "";
            NotesBox.IsVisible = false;
            PayCash.IsChecked = true;
            CreditSaleCheck.IsChecked = false;
            CreditCustomerNameBox.Text = "";
            CreditCustomerNameBox.IsVisible = false;
            BuildCategoryButtons();
            RefreshTotals();
            ShowStatus("Sale completed.");

            var receipt = await receiptService.BuildReceiptAsync(
                sale, customerName, paymentMethod, amountReceived, changeDue, totalDiscount);
            var win = new ReceiptWindow(receipt, receiptService);
            win.Show();
        };
    }

    private void BuildCategoryButtons()
    {
        CategoryPanel.Children.Clear();
        foreach (var cat in _vm.Categories)
        {
            var btn = new Button
            {
                Content = cat,
                Height = 36,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                Padding = new Avalonia.Thickness(10, 0),
                CornerRadius = new Avalonia.CornerRadius(6),
                Background = cat == _selectedCategory ? Avalonia.Media.Brush.Parse("#0F2A43") : Avalonia.Media.Brushes.Transparent,
                Foreground = cat == _selectedCategory ? Avalonia.Media.Brushes.White : Avalonia.Media.Brush.Parse("#334155")
            };
            btn.Click += (_, _) =>
            {
                _selectedCategory = cat;
                RunFilter();
                BuildCategoryButtons();
            };
            CategoryPanel.Children.Add(btn);
        }
    }

    private void RunFilter() => _vm.ApplyFilter(SearchBox.Text, _selectedCategory);

    private void RefreshTotals()
    {
        SubtotalText.Text = $"${_vm.Subtotal:F2}";
        DiscountText.Text = $"-${_vm.TotalDiscount:F2}";
        TaxText.Text = $"${_vm.TaxAmount:F2}";
        TotalText.Text = $"${_vm.Total:F2}";
        ChangeText.Text = $"${_vm.ChangeDue:F2}";
    }

    private void ShowStatus(string message) => StatusText.Text = message;
}
