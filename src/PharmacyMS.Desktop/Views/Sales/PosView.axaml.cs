using Avalonia.VisualTree;
using System.Collections.Generic;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using PharmacyMS.Application.Interfaces.Services;
using PharmacyMS.Desktop.ViewModels;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Desktop.Views.Sales;

public partial class PosView : UserControl
{
    private readonly PosViewModel _vm;
    private string? _selectedCategory = "All";
    private decimal _slshRate = 0;

    public PosView() { InitializeComponent(); }
    public PosView(PosViewModel vm)
    {
        InitializeComponent();
        _vm = vm;

        _ = LoadSlshRateAsync();

        MedicineGrid.ItemsSource = _vm.AvailableMedicines;
        CartGrid.ItemsSource = _vm.Cart;
        MedicineGrid.LoadingRow += (_, e) => e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        CartGrid.LoadingRow += (_, e) => e.Row.Header = (e.Row.GetIndex() + 1).ToString();

        var unitOptions = new List<string>
        {
            "Box", "Bottle", "Packet", "Pcs", "Vial", "Ampoule",
            "Sachet", "Strip", "Tube", "Syringe", "Roll", "Other"
        };

        CartGrid.LoadingRow += (_, e) =>
        {
            if (e.Row.DataContext is not CartLine line) return;
            e.Row.Loaded += (_, _) =>
            {
                var combo = e.Row.FindDescendantOfType<ComboBox>();
                if (combo == null) return;
                combo.ItemsSource = unitOptions;
                combo.SelectedItem = line.Unit;
            };
        };

        CartGrid.AddHandler(ComboBox.SelectionChangedEvent, (object? sender, Avalonia.Controls.SelectionChangedEventArgs e) =>
        {
            if (e.Source is not ComboBox combo) return;
            if (combo.Name != "UnitCombo") return;
            if (combo.DataContext is not CartLine line) return;
            if (combo.SelectedItem is string unit) line.Unit = unit;
        }, Avalonia.Interactivity.RoutingStrategies.Bubble);
        CustomerCombo.ItemsSource = _vm.Customers;
        CustomerCombo.DisplayMemberBinding = new Avalonia.Data.Binding("Name");
        CustomerCombo.SelectionChanged += (_, _) =>
        {
            var selected = CustomerCombo.SelectedItem as PharmacyMS.Domain.Entities.Customer;
            var isRealCustomer = selected != null && selected.Id != 0 && selected.Name != "Walk-in Customer";

            // Only allow credit sale if a real customer is selected
            CreditSaleCheck.IsEnabled = isRealCustomer;
            if (!isRealCustomer)
            {
                CreditSaleCheck.IsChecked = false;
                _vm.IsCreditSale = false;
                CreditCustomerCombo.IsVisible = false;
            }

            if (_vm.IsCreditSale && isRealCustomer)
            {
                // Sync credit combo with main combo
                for (int i = 0; i < CreditCustomerCombo.Items.Count; i++)
                {
                    if (CreditCustomerCombo.Items[i] is PharmacyMS.Domain.Entities.Customer cc && cc.Id == selected!.Id)
                    {
                        CreditCustomerCombo.SelectedIndex = i;
                        break;
                    }
                }
            }
        };

        AttachedToVisualTree += async (_, _) =>
        {
            await _vm.LoadAsync();
            BuildCategoryButtons();
            if (CustomerCombo.ItemsSource is System.Collections.ObjectModel.ObservableCollection<Customer> c && c.Count > 0)
                CustomerCombo.SelectedIndex = 0;
            RefreshTotals();
                if (_vm.Cart.Count > 0) CartGrid.ScrollIntoView(_vm.Cart[_vm.Cart.Count - 1], null);
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
                if (_vm.Cart.Count > 0) CartGrid.ScrollIntoView(_vm.Cart[_vm.Cart.Count - 1], null);
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
                if (_vm.Cart.Count > 0) CartGrid.ScrollIntoView(_vm.Cart[_vm.Cart.Count - 1], null);
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
                if (_vm.Cart.Count > 0) CartGrid.ScrollIntoView(_vm.Cart[_vm.Cart.Count - 1], null);
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
                if (_vm.Cart.Count > 0) CartGrid.ScrollIntoView(_vm.Cart[_vm.Cart.Count - 1], null);
            }
        };

        PrescriptionCheck.IsCheckedChanged += (_, _) => _vm.IsPrescription = PrescriptionCheck.IsChecked == true;

        CreditSaleCheck.IsCheckedChanged += (_, _) =>
        {
            _vm.IsCreditSale = CreditSaleCheck.IsChecked == true;
            CreditCustomerCombo.IsVisible = _vm.IsCreditSale;
            if (_vm.IsCreditSale)
            {
                CreditCustomerCombo.ItemsSource = _vm.Customers
                    .Where(c => c.Name != "Walk-in Customer" && c.IsActive)
                    .ToList();
                CreditCustomerCombo.SelectedIndex = -1;
            }
        };
        CreditCustomerCombo.DisplayMemberBinding = new Avalonia.Data.Binding("Name");
        CreditCustomerCombo.SelectionChanged += (_, _) =>
        {
            if (CreditCustomerCombo.SelectedItem is PharmacyMS.Domain.Entities.Customer c)
            {
                _vm.CreditCustomerName = c.Name;
                _vm.SelectedCustomer = c;
                // Sync with main customer dropdown
                for (int i = 0; i < CustomerCombo.Items.Count; i++)
                {
                    if (CustomerCombo.Items[i] is PharmacyMS.Domain.Entities.Customer mc && mc.Id == c.Id)
                    {
                        CustomerCombo.SelectedIndex = i;
                        break;
                    }
                }
            }
        };

        // Populate credit combo without Walk-in Customer when customers load
        _vm.Customers.CollectionChanged += (_, _) =>
        {
            CreditCustomerCombo.ItemsSource = _vm.Customers
                .Where(c => c.Name != "Walk-in Customer" && c.IsActive)
                .ToList();
        };

        NotesButton.Click += (_, _) => NotesBox.IsVisible = !NotesBox.IsVisible;
        NotesBox.PropertyChanged += (_, e) =>
        {
            if (e.Property == TextBox.TextProperty) _vm.Notes = NotesBox.Text ?? "";
        };

        PayCash.IsCheckedChanged += (_, _) => { if (PayCash.IsChecked == true) _vm.PaymentMethod = "Cash"; };
        PayCard.IsCheckedChanged += (_, _) => { if (PayCard.IsChecked == true) _vm.PaymentMethod = "ZAAD Merchant"; };
        PayMobile.IsCheckedChanged += (_, _) => { if (PayMobile.IsChecked == true) _vm.PaymentMethod = "E-DAHAB"; };
        PayInsurance.IsCheckedChanged += (_, _) => { if (PayInsurance.IsChecked == true) _vm.PaymentMethod = "Bank Transfer"; };

        ClearCartButton.Click += (_, _) =>
        {
            _vm.ClearCart();
            AmountReceivedBox.Text = "0";
            PrescriptionCheck.IsChecked = false;
            NotesBox.Text = "";
            NotesBox.IsVisible = false;
            PayCash.IsChecked = true;
            RefreshTotals();
                if (_vm.Cart.Count > 0) CartGrid.ScrollIntoView(_vm.Cart[_vm.Cart.Count - 1], null);
        };

        HoldSaleButton.Click += (_, _) =>
        {
            var label = $"Hold {DateTime.Now:HH:mm:ss}";
            _vm.HoldCurrentSale(label);
            AmountReceivedBox.Text = "0";
            RefreshTotals();
                if (_vm.Cart.Count > 0) CartGrid.ScrollIntoView(_vm.Cart[_vm.Cart.Count - 1], null);
            ShowStatus($"Sale held as \"{label}\".");
        };

        RecallSaleButton.Click += (_, _) =>
        {
            if (_vm.HeldSales.Count == 0) { ShowStatus("No held sales."); return; }
            _vm.RecallHeldSale(_vm.HeldSales.Last());
            RefreshTotals();
                if (_vm.Cart.Count > 0) CartGrid.ScrollIntoView(_vm.Cart[_vm.Cart.Count - 1], null);
            ShowStatus("");
        };

        CalculatorButton.Click += (_, _) => new CalculatorWindow().Show();
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
                var creditCustomer = CreditCustomerCombo.SelectedItem as PharmacyMS.Domain.Entities.Customer;
                if (creditCustomer == null)
                {
                    ShowStatus("Credit sale requires a real customer — please select one from the dropdown.");
                    return;
                }
                // Set the selected customer on the VM
                _vm.SelectedCustomer = creditCustomer;
                _vm.CreditCustomerName = creditCustomer.Name;

                var selected = creditCustomer;
                var hasRealCustomer = true;

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

            Sale sale;
            try
            {
                sale = await _vm.CheckoutAsync();
            }
            catch (Exception ex)
            {
                ShowStatus($"Sale failed: {ex.Message}");
                // Refresh stock/cart state so the cashier sees current reality
                // (important on shared cloud DB — another PC may have just sold the item).
                await _vm.LoadAsync();
                RunFilter();
                RefreshTotals();
                if (_vm.Cart.Count > 0) CartGrid.ScrollIntoView(_vm.Cart[_vm.Cart.Count - 1], null);
                return;
            }

            if (sale == null)
            {
                ShowStatus("Sale failed — please try again.");
                return;
            }

            AmountReceivedBox.Text = "0";
            PrescriptionCheck.IsChecked = false;
            NotesBox.Text = "";
            NotesBox.IsVisible = false;
            PayCash.IsChecked = true;
            CreditSaleCheck.IsChecked = false;
            CreditCustomerCombo.SelectedIndex = -1;
            CreditCustomerCombo.IsVisible = false;
            BuildCategoryButtons();
            RefreshTotals();
                if (_vm.Cart.Count > 0) CartGrid.ScrollIntoView(_vm.Cart[_vm.Cart.Count - 1], null);
            ShowStatus("Sale completed.");

            try
            {
                var receipt = await receiptService.BuildReceiptAsync(
                    sale, customerName, paymentMethod, amountReceived, changeDue, totalDiscount);
                var win = new ReceiptWindow(receipt, receiptService);
                win.Show();
            }
            catch (Exception ex)
            {
                ShowStatus($"Receipt error: {ex.Message}");
            }
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

    private async Task LoadSlshRateAsync()
    {
        var settingsService = Program.Services.GetRequiredService<IAppSettingsService>();
        _slshRate = await settingsService.GetSlshExchangeRateAsync();
        RefreshTotals();
                if (_vm.Cart.Count > 0) CartGrid.ScrollIntoView(_vm.Cart[_vm.Cart.Count - 1], null);
    }

    private void RefreshTotals()
    {
        SubtotalText.Text = $"${_vm.Subtotal:F2}";
        DiscountText.Text = $"-${_vm.TotalDiscount:F2}";
        TaxText.Text = $"${_vm.TaxAmount:F2}";
        TotalText.Text = $"${_vm.Total:F2}";
        ChangeText.Text = $"${_vm.ChangeDue:F2}";

        if (_slshRate > 0)
        {
            var localAmount = _vm.Total * _slshRate;
            TotalLocalText.Text = $"≈ SLSH {localAmount:N0}";
        }
        else
        {
            TotalLocalText.Text = "";
        }
    }

    private void ShowStatus(string message) => StatusText.Text = message;
}
