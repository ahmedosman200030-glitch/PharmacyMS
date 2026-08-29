using Microsoft.Extensions.DependencyInjection;
using Avalonia.Controls;
using PharmacyMS.Desktop.Views.Accounting;
using PharmacyMS.Desktop.Views.Approvals;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Application.Interfaces.Services;
using PharmacyMS.Application.Services;
using PharmacyMS.Desktop.ViewModels;
using PharmacyMS.Desktop.Views.Inventory;
using PharmacyMS.Desktop.Views.Sales;
using Avalonia.Threading;
using Avalonia.Media;

namespace PharmacyMS.Desktop.Views.Shell;

public partial class MainShell : UserControl
{
    private static readonly IBrush ActiveBrush = new SolidColorBrush(Color.Parse("#DC2626"));
    private static readonly IBrush InactiveBrush = Brushes.Transparent;
    private static readonly IBrush ActiveForeground = Brushes.White;
    private static readonly IBrush InactiveForeground = new SolidColorBrush(Color.Parse("#94A3B8"));

    private static readonly Dictionary<Button, string> PageTitles = new();

    private HashSet<string> _seenAlertKeys = new();
    private bool _firstStatsLoad = true;
    private DispatcherTimer? _statsTimer;
    private bool _posExpanded = false;
    private bool _purchasesExpanded = false;
    private bool _usersExpanded = false;
    private readonly Action _onLogout;

    private Button[] NavButtons => new[]
    {
        DashboardButton, InventoryButton, SalesReturnButton, CategoriesButton, SuppliersButton,
        NewPurchaseButton, PurchaseHistoryButton, PurchaseInvoicesButton, PurchaseReportsButton,
        PointOfSaleButton, DailyClosingButton, SalesHistoryButton, CreditSalesButton, CustomersButton, SettingsButton, AllUsersButton, UserActivityButton
    };

    public MainShell(Action onLogout)
    {
        InitializeComponent();
        _onLogout = onLogout;

        var user = SessionManager.CurrentUser;
        if (user != null)
        {
            UserNameText.Text = user.FullName;
            UserRoleText.Text = user.Role.ToString();
            AvatarInitialsText.Text = GetInitials(user.FullName);

            MenuUserName.Text = user.FullName;
            MenuUserRole.Text = user.Role.ToString();
            HeaderUserName.Text = user.FullName;
            HeaderUserRole.Text = user.Role.ToString();
            HeaderAvatarInitials.Text = GetInitials(user.FullName);
            FlyoutAvatarInitials.Text = GetInitials(user.FullName);
            FlyoutFullName.Text = user.FullName;
            FlyoutRole.Text = user.Role.ToString();
            FlyoutSessionTime.Text = DateTime.Now.ToString("hh:mm tt");

            if (!string.IsNullOrWhiteSpace(user.AvatarPath) && File.Exists(user.AvatarPath))
            {
                AvatarImage.Source = new Avalonia.Media.Imaging.Bitmap(user.AvatarPath);
                AvatarImage.IsVisible = true;
                AvatarInitialsText.IsVisible = false;
            }
        }

        _ = LoadBrandingAsync();
        _ = LoadHeaderStatsAsync();

        _statsTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(60) };
        _statsTimer.Tick += async (_, _) => await LoadHeaderStatsAsync();
        _statsTimer.Start();

        // Enforce permissions on sidebar
        if (!SessionManager.IsAdmin)
        {
            UsersButton.IsVisible = false;
            SettingsButton.IsVisible = false;
            PendingApprovalsButton.IsVisible = false;
        }
        else
        {
            MyStatusButton.IsVisible = false;
        }

        if (!SessionManager.CanViewReports)

        if (!SessionManager.CanManageMedicines)
            InventoryButton.IsVisible = false;

        if (!SessionManager.CanManageCategories)
            CategoriesButton.IsVisible = false;

        if (!SessionManager.CanManageSuppliers)
            SuppliersButton.IsVisible = false;

        if (!SessionManager.CanManagePurchases)
            PurchasesButton.IsVisible = false;

        if (!SessionManager.CanManageSales)
        {
            PosButton.IsVisible = false;
            QuickPosButton.IsVisible = false;
        }

        if (!SessionManager.CanManageCustomers)
            CustomersButton.IsVisible = false;

        AvatarButton.Click += async (_, _) => await UploadAvatarAsync();

        // Load Dashboard by default, highlighted
        SetActive(DashboardButton);
        PageTitleText.Text = "Dashboard";
        ShowDashboard();

        LogoutButton.Click += (_, _) => DoLogout();
        MenuLogoutButton.Click += (_, _) => DoLogout();

        MenuPreferencesButton.Click += (_, _) =>
        {
            SetActive(SettingsButton);
            PageTitleText.Text = "Settings";
            OpenSettings();
        };

        SettingsQuickButton.Click += (_, _) =>
        {
            SetActive(SettingsButton);
            PageTitleText.Text = "Settings";
            OpenSettings();
        };

        DashboardButton.Click += (_, _) => { SetActive(DashboardButton); PageTitleText.Text = "Dashboard"; ShowDashboard(); };

        InventoryButton.Click += (_, _) =>
        {
            SetActive(InventoryButton);
            PageTitleText.Text = "Inventory";
            var repo = Program.Services.GetRequiredService<IMedicineRepository>();
            var vm = new InventoryViewModel(repo);
            MainContent.Content = new PharmacyMS.Desktop.Views.Inventory.InventoryView(vm);
        };


        CategoriesButton.Click += (_, _) =>
        {
            SetActive(CategoriesButton);
            PageTitleText.Text = "Categories";
            var repo = Program.Services.GetRequiredService<ICategoryRepository>();
            var vm = new PharmacyMS.Desktop.ViewModels.CategoriesViewModel(repo);
            MainContent.Content = new PharmacyMS.Desktop.Views.Categories.CategoriesView(vm);
        };

        SuppliersButton.Click += (_, _) =>
        {
            SetActive(SuppliersButton);
            PageTitleText.Text = "Suppliers";
            var repo = Program.Services.GetRequiredService<ISupplierRepository>();
            var vm = new PharmacyMS.Desktop.ViewModels.SuppliersViewModel(repo);
            MainContent.Content = new PharmacyMS.Desktop.Views.Suppliers.SuppliersView(vm);
        };

        PurchasesButton.Click += (_, _) =>
        {
            _purchasesExpanded = !_purchasesExpanded;
            PurchasesSubPanel.IsVisible = _purchasesExpanded;
        };

        NewPurchaseButton.Click += (_, _) =>
        {
            SetActive(NewPurchaseButton);
            PageTitleText.Text = "Purchase Orders";
            var medicineRepo = Program.Services.GetRequiredService<IMedicineRepository>();
            var orderRepo = Program.Services.GetRequiredService<PharmacyMS.Application.Interfaces.Repositories.IPurchaseOrderRepository>();
            var supplierRepo = Program.Services.GetRequiredService<PharmacyMS.Application.Interfaces.Repositories.ISupplierRepository>();
            var pdfService = Program.Services.GetRequiredService<PharmacyMS.Application.Interfaces.Services.IPurchaseOrderPdfService>();
            var vm = new PharmacyMS.Desktop.ViewModels.PurchaseOrderViewModel(medicineRepo, orderRepo, supplierRepo, pdfService);
            MainContent.Content = new PharmacyMS.Desktop.Views.Purchases.PurchaseOrderView(vm);
        };

        ReceivedGoodsButton.Click += (_, _) =>
        {
            SetActive(ReceivedGoodsButton);
            PageTitleText.Text = "Received";
            var orderRepo = Program.Services.GetRequiredService<PharmacyMS.Application.Interfaces.Repositories.IPurchaseOrderRepository>();
            var receiptRepo = Program.Services.GetRequiredService<PharmacyMS.Application.Interfaces.Repositories.IGoodsReceiptRepository>();
            var purchaseRepo = Program.Services.GetRequiredService<PharmacyMS.Application.Interfaces.Repositories.IPurchaseRepository>();
            var vm = new PharmacyMS.Desktop.ViewModels.ReceiveGoodsViewModel(orderRepo, receiptRepo, purchaseRepo);
            MainContent.Content = new PharmacyMS.Desktop.Views.Purchases.ReceiveGoodsView(vm);
        };

        PurchaseHistoryButton.Click += (_, _) =>
        {
            SetActive(PurchaseHistoryButton);
            PageTitleText.Text = "Supplier Bills";
            var purchaseRepo = Program.Services.GetRequiredService<PharmacyMS.Application.Interfaces.Repositories.IPurchaseRepository>();
            var vm = new PharmacyMS.Desktop.ViewModels.PurchaseHistoryViewModel(purchaseRepo);
            MainContent.Content = new PharmacyMS.Desktop.Views.Purchases.PurchaseHistoryView(vm);
        };

        PurchaseInvoicesButton.Click += (_, _) =>
        {
            SetActive(PurchaseInvoicesButton);
            PageTitleText.Text = "Supplier Payments";
            var purchaseRepo = Program.Services.GetRequiredService<PharmacyMS.Application.Interfaces.Repositories.IPurchaseRepository>();
            var vm = new PharmacyMS.Desktop.ViewModels.PurchaseInvoiceViewModel(purchaseRepo);
            MainContent.Content = new PharmacyMS.Desktop.Views.Purchases.PurchaseInvoiceView(vm);
        };

        PurchaseReportsButton.Click += (_, _) =>
        {
            SetActive(PurchaseReportsButton);
            PageTitleText.Text = "Purchase Reports";
            var purchaseRepo = Program.Services.GetRequiredService<PharmacyMS.Application.Interfaces.Repositories.IPurchaseRepository>();
            var vm = new PharmacyMS.Desktop.ViewModels.PurchaseReportsViewModel(purchaseRepo);
            MainContent.Content = new PharmacyMS.Desktop.Views.Purchases.PurchaseReportsView(vm);
        };

        PosButton.Click += (_, _) =>
        {
            _posExpanded = !_posExpanded;
            PosSubPanel.IsVisible = _posExpanded;
        };

        SalesReturnButton.Click += (_, _) =>
        {
            SetActive(SalesReturnButton);
            PageTitleText.Text = "Sales Returns";
            var saleRepo = Program.Services.GetRequiredService<ISaleRepository>();
            var returnRepo = Program.Services.GetRequiredService<PharmacyMS.Application.Interfaces.Repositories.ISaleReturnRepository>();
            var vm = new PharmacyMS.Desktop.ViewModels.SalesReturnViewModel(saleRepo, returnRepo);
            MainContent.Content = new PharmacyMS.Desktop.Views.Sales.SalesReturnView(vm);
        };

        PointOfSaleButton.Click += (_, _) =>
        {
            SetActive(PointOfSaleButton);
            PageTitleText.Text = "Sales / POS";
            var medicineRepo = Program.Services.GetRequiredService<IMedicineRepository>();
            var saleRepo = Program.Services.GetRequiredService<ISaleRepository>();
            var settingsService = Program.Services.GetRequiredService<IAppSettingsService>();
            var customerRepo = Program.Services.GetRequiredService<ICustomerRepository>();
            var soundService = Program.Services.GetRequiredService<ISoundService>();
            var vm = new PosViewModel(medicineRepo, saleRepo, settingsService, customerRepo, soundService);
            MainContent.Content = new PharmacyMS.Desktop.Views.Sales.PosView(vm);
        };

        QuickPosButton.Click += (_, _) =>
        {
            _posExpanded = true;
            PosSubPanel.IsVisible = true;
            SetActive(PointOfSaleButton);
            PageTitleText.Text = "Sales / POS";
            var medicineRepo = Program.Services.GetRequiredService<IMedicineRepository>();
            var saleRepo = Program.Services.GetRequiredService<ISaleRepository>();
            var settingsService = Program.Services.GetRequiredService<IAppSettingsService>();
            var customerRepo = Program.Services.GetRequiredService<ICustomerRepository>();
            var soundService = Program.Services.GetRequiredService<ISoundService>();
            var vm = new PosViewModel(medicineRepo, saleRepo, settingsService, customerRepo, soundService);
            MainContent.Content = new PharmacyMS.Desktop.Views.Sales.PosView(vm);
        };

        DailyClosingButton.Click += async (_, _) =>
        {
            SetActive(DailyClosingButton);
            PageTitleText.Text = "Daily Closing";
            var saleRepo2 = Program.Services.GetRequiredService<ISaleRepository>();
            var closingRepo = Program.Services.GetRequiredService<IDailyClosingRepository>();
            var vm2 = new PharmacyMS.Desktop.ViewModels.DailyClosingViewModel(saleRepo2, closingRepo);
            var view = new PharmacyMS.Desktop.Views.Sales.DailyClosingView(vm2);
            MainContent.Content = view;
            await vm2.LoadAsync();
        };

        CustomersButton.Click += (_, _) =>
        {
            SetActive(CustomersButton);
            PageTitleText.Text = "Customers";
            var repo = Program.Services.GetRequiredService<ICustomerRepository>();
            var vm = new PharmacyMS.Desktop.ViewModels.CustomersViewModel(repo);
            MainContent.Content = new PharmacyMS.Desktop.Views.Customers.CustomersView(vm);
        };

        bool _accountingExpanded = false;
        AccountingButton.Click += (_, _) =>
        {
            _accountingExpanded = !_accountingExpanded;
            AccountingSubPanel.IsVisible = _accountingExpanded;
        };

        void OpenAccounting(int tab = 0)
        {
            SetActive(AccountingButton);
            PageTitleText.Text = "Accounting";
            var saleRepo = Program.Services.GetRequiredService<ISaleRepository>();
            var purchaseRepo = Program.Services.GetRequiredService<IPurchaseRepository>();
            var expenseRepo = Program.Services.GetRequiredService<IExpenseRepository>();
            var customerRepo = Program.Services.GetRequiredService<ICustomerRepository>();
            var medicineRepo = Program.Services.GetRequiredService<IMedicineRepository>();
            var saleReturnRepo = Program.Services.GetRequiredService<PharmacyMS.Application.Interfaces.Repositories.ISaleReturnRepository>();
            var vm = new PharmacyMS.Desktop.ViewModels.AccountingViewModel(saleRepo, purchaseRepo, expenseRepo, customerRepo, medicineRepo, saleReturnRepo);
            var view = new AccountingView(vm, tab);
            MainContent.Content = view;
        }

        AccOverviewButton.Click += (_, _) => { SetActive(AccOverviewButton); OpenAccounting(0); };
        AccIncomeButton.Click += (_, _) =>
        {
            SetActive(AccIncomeButton);
            PageTitleText.Text = "Income";
            var saleRepoForIncome = Program.Services.GetRequiredService<ISaleRepository>();
            var otherIncomeRepoForIncome = Program.Services.GetRequiredService<PharmacyMS.Application.Interfaces.Repositories.IOtherIncomeRepository>();
            var incomeVm = new PharmacyMS.Desktop.ViewModels.IncomeViewModel(saleRepoForIncome, otherIncomeRepoForIncome);
            MainContent.Content = new IncomeView(incomeVm);
        };
        AccExpensesButton.Click += (_, _) =>
        {
            SetActive(AccExpensesButton);
            PageTitleText.Text = "Expenses";
            var expenseRepo = Program.Services.GetRequiredService<IExpenseRepository>();
            var pendingExpenseRepo = Program.Services.GetRequiredService<IPendingExpenseRepository>();
            var expensesVm = new PharmacyMS.Desktop.ViewModels.ExpensesViewModel(expenseRepo, pendingExpenseRepo);
            MainContent.Content = new ExpensesView(expensesVm);
        };
        AccReceivablesButton.Click += (_, _) =>
        {
            SetActive(AccReceivablesButton);
            PageTitleText.Text = "Receivables";
            var saleRepoForRec = Program.Services.GetRequiredService<ISaleRepository>();
            var customerRepoForRec = Program.Services.GetRequiredService<ICustomerRepository>();
            var pendingSalePaymentRepo = Program.Services.GetRequiredService<IPendingSalePaymentRepository>();
            var recVm = new PharmacyMS.Desktop.ViewModels.ReceivablesViewModel(saleRepoForRec, customerRepoForRec, pendingSalePaymentRepo);
            MainContent.Content = new ReceivablesView(recVm);
        };
        AccPayablesButton.Click += (_, _) =>
        {
            SetActive(AccPayablesButton);
            PageTitleText.Text = "Payables";
            var purchaseRepoForPay = Program.Services.GetRequiredService<IPurchaseRepository>();
            var supplierRepoForPay = Program.Services.GetRequiredService<ISupplierRepository>();
            var payVm = new PharmacyMS.Desktop.ViewModels.PayablesViewModel(purchaseRepoForPay, supplierRepoForPay);
            MainContent.Content = new PayablesView(payVm);
        };
        AccCashFlowButton.Click += async (_, _) =>
        {
            SetActive(AccCashFlowButton);
            PageTitleText.Text = "Cash Flow";
            var saleRepoForCF = Program.Services.GetRequiredService<ISaleRepository>();
            var purchaseRepoForCF = Program.Services.GetRequiredService<IPurchaseRepository>();
            var expenseRepoForCF = Program.Services.GetRequiredService<IExpenseRepository>();
            var cfVm = new PharmacyMS.Desktop.ViewModels.CashFlowViewModel(saleRepoForCF, purchaseRepoForCF, expenseRepoForCF);
            MainContent.Content = new CashFlowView(cfVm);
        };
        AccPLButton.Click += async (_, _) =>
        {
            SetActive(AccPLButton);
            PageTitleText.Text = "Profit & Loss";
            var saleRepoForPL = Program.Services.GetRequiredService<ISaleRepository>();
            var purchaseRepoForPL = Program.Services.GetRequiredService<IPurchaseRepository>();
            var expenseRepoForPL = Program.Services.GetRequiredService<IExpenseRepository>();
            var reportRepoForPL = Program.Services.GetRequiredService<IReportRepository>();
            var saleReturnRepoForPL = Program.Services.GetRequiredService<PharmacyMS.Application.Interfaces.Repositories.ISaleReturnRepository>();
            var plVm = new PharmacyMS.Desktop.ViewModels.PLViewModel(saleRepoForPL, purchaseRepoForPL, expenseRepoForPL, reportRepoForPL, saleReturnRepoForPL);
            MainContent.Content = new PLView(plVm);
        };
        AccReportsButton.Click += (_, _) =>
        {
            SetActive(AccReportsButton);
            PageTitleText.Text = "Financial Reports";
            var accRepo = Program.Services.GetRequiredService<IReportRepository>();
            var accReportsVm = new PharmacyMS.Desktop.ViewModels.ReportsViewModel(accRepo);
            var accBrandingService = Program.Services.GetRequiredService<IBrandingService>();
            MainContent.Content = new PharmacyMS.Desktop.Views.Reports.ReportsView(accReportsVm, accBrandingService);
        };

        SalesHistoryButton.Click += (_, _) =>
        {
            SetActive(SalesHistoryButton);
            PageTitleText.Text = "Sales History";
            var saleRepo = Program.Services.GetRequiredService<ISaleRepository>();
            var customerRepo = Program.Services.GetRequiredService<ICustomerRepository>();
            var vm = new PharmacyMS.Desktop.ViewModels.SalesHistoryViewModel(saleRepo, customerRepo);
            var brandingService = Program.Services.GetRequiredService<IBrandingService>();
            MainContent.Content = new PharmacyMS.Desktop.Views.Sales.SalesHistoryView(vm, brandingService);
        };

        CreditSalesButton.Click += (_, _) =>
        {
            SetActive(CreditSalesButton);
            PageTitleText.Text = "Credit Sales";
            var saleRepo = Program.Services.GetRequiredService<ISaleRepository>();
            var customerRepo2 = Program.Services.GetRequiredService<ICustomerRepository>();
            var pendingPayRepo = Program.Services.GetRequiredService<IPendingSalePaymentRepository>();
            var vm = new PharmacyMS.Desktop.ViewModels.CreditSalesViewModel(saleRepo, customerRepo2, pendingPayRepo);
            var brandingService2 = Program.Services.GetRequiredService<IBrandingService>();
            MainContent.Content = new PharmacyMS.Desktop.Views.Sales.CreditSalesView(vm, brandingService2);
        };

        UsersButton.Click += (_, _) =>
        {
            _usersExpanded = !_usersExpanded;
            UsersSubPanel.IsVisible = _usersExpanded;
        };

        AllUsersButton.Click += (_, _) =>
        {
            SetActive(AllUsersButton);
            PageTitleText.Text = "Users";
            var repo = Program.Services.GetRequiredService<IUserRepository>();
            var vm = new PharmacyMS.Desktop.ViewModels.UsersViewModel(repo);
            MainContent.Content = new PharmacyMS.Desktop.Views.Users.UsersView(vm);
        };

        UserActivityButton.Click += (_, _) =>
        {
            SetActive(UserActivityButton);
            PageTitleText.Text = "User Activity";
            MainContent.Content = new PharmacyMS.Desktop.Views.Reports.UserActivityView();
        };

        PendingApprovalsButton.Click += async (_, _) =>
        {
            SetActive(PendingApprovalsButton);
            PageTitleText.Text = "Pending Approvals";
            var customerRepo = Program.Services.GetRequiredService<ICustomerRepository>();
            var supplierRepo = Program.Services.GetRequiredService<ISupplierRepository>();
            var purchaseRepo = Program.Services.GetRequiredService<IPurchaseRepository>();
            var goodsReceiptRepo = Program.Services.GetRequiredService<PharmacyMS.Application.Interfaces.Repositories.IGoodsReceiptRepository>();
            var paymentRepo = Program.Services.GetRequiredService<PharmacyMS.Application.Interfaces.Repositories.IPendingSalePaymentRepository>();
            var pendingExpenseRepo = Program.Services.GetRequiredService<PharmacyMS.Application.Interfaces.Repositories.IPendingExpenseRepository>();
            var saleRepo = Program.Services.GetRequiredService<PharmacyMS.Application.Interfaces.Repositories.ISaleRepository>();
            var realExpenseRepo = Program.Services.GetRequiredService<PharmacyMS.Application.Interfaces.Repositories.IExpenseRepository>();
            var vm = new PharmacyMS.Desktop.ViewModels.PendingApprovalsViewModel(customerRepo, supplierRepo, purchaseRepo, goodsReceiptRepo, paymentRepo, pendingExpenseRepo, saleRepo, realExpenseRepo);
            MainContent.Content = new PendingApprovalsView(vm);
        };

        MyStatusButton.Click += async (_, _) =>
        {
            SetActive(MyStatusButton);
            PageTitleText.Text = "My Status";
            var myPaymentRepo = Program.Services.GetRequiredService<PharmacyMS.Application.Interfaces.Repositories.IPendingSalePaymentRepository>();
            var myExpenseRepo = Program.Services.GetRequiredService<PharmacyMS.Application.Interfaces.Repositories.IPendingExpenseRepository>();
            var myReceiptRepo = Program.Services.GetRequiredService<PharmacyMS.Application.Interfaces.Repositories.IGoodsReceiptRepository>();
            var myCustomerRepo = Program.Services.GetRequiredService<ICustomerRepository>();
            var mySupplierRepo = Program.Services.GetRequiredService<ISupplierRepository>();
            var myVm = new PharmacyMS.Desktop.ViewModels.MySubmissionsViewModel(myPaymentRepo, myExpenseRepo, myReceiptRepo, myCustomerRepo, mySupplierRepo);
            MainContent.Content = new PharmacyMS.Desktop.Views.Approvals.MySubmissionsView(myVm);
        };

        SettingsButton.Click += (_, _) =>
        {
            SetActive(SettingsButton);
            PageTitleText.Text = "Settings";
            OpenSettings();
        };

        GlobalSearchBox.TextChanged += async (_, _) => await RunGlobalSearchAsync();

    }

    private async Task RunGlobalSearchAsync()
    {
        var term = GlobalSearchBox.Text?.Trim() ?? "";
        if (term.Length < 2)
        {
            SearchResultsPopup.IsOpen = false;
            return;
        }

        var results = new List<PharmacyMS.Desktop.ViewModels.SearchResultItem>();

        try
        {
            var medicineRepo = Program.Services.GetRequiredService<IMedicineRepository>();
            var medicines = await medicineRepo.SearchAsync(term);
            foreach (var m in medicines.Take(5))
            {
                results.Add(new PharmacyMS.Desktop.ViewModels.SearchResultItem
                {
                    Icon = "💊",
                    Title = m.Name,
                    Subtitle = "Medicine — Stock: " + m.QuantityInStock,
                    NavigateCommand = new PharmacyMS.Desktop.ViewModels.RelayCommand(() =>
                    {
                        SearchResultsPopup.IsOpen = false;
                        GlobalSearchBox.Text = "";
                        NavigateToInventory(null);
                        return Task.CompletedTask;
                    })
                });
            }

            var customerRepo = Program.Services.GetRequiredService<ICustomerRepository>();
            var customers = await customerRepo.SearchAsync(term);
            foreach (var c in customers.Take(5))
            {
                results.Add(new PharmacyMS.Desktop.ViewModels.SearchResultItem
                {
                    Icon = "👤",
                    Title = c.Name,
                    Subtitle = "Customer" + (string.IsNullOrWhiteSpace(c.Phone) ? "" : " — " + c.Phone),
                    NavigateCommand = new PharmacyMS.Desktop.ViewModels.RelayCommand(() =>
                    {
                        SearchResultsPopup.IsOpen = false;
                        GlobalSearchBox.Text = "";
                        CustomersButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));
                        return Task.CompletedTask;
                    })
                });
            }

            var saleRepo = Program.Services.GetRequiredService<ISaleRepository>();
            var allSales = await saleRepo.GetAllAsync();
            var sales = allSales
                .Where(s => s.InvoiceNumber.Contains(term, StringComparison.OrdinalIgnoreCase)
                         || s.CustomerName.Contains(term, StringComparison.OrdinalIgnoreCase))
                .Take(5);
            foreach (var s in sales)
            {
                results.Add(new PharmacyMS.Desktop.ViewModels.SearchResultItem
                {
                    Icon = "🧾",
                    Title = s.InvoiceNumber,
                    Subtitle = "Invoice — " + s.CustomerName + " — $" + s.TotalAmount.ToString("0.00"),
                    NavigateCommand = new PharmacyMS.Desktop.ViewModels.RelayCommand(() =>
                    {
                        SearchResultsPopup.IsOpen = false;
                        GlobalSearchBox.Text = "";
                        SalesHistoryButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));
                        return Task.CompletedTask;
                    })
                });
            }

            if (results.Count == 0)
            {
                results.Add(new PharmacyMS.Desktop.ViewModels.SearchResultItem
                {
                    Icon = "🚫",
                    Title = "No results found",
                    Subtitle = "Try a different search term",
                    NavigateCommand = new PharmacyMS.Desktop.ViewModels.RelayCommand(() => Task.CompletedTask)
                });
            }

            SearchResultsList.ItemsSource = results;
            SearchResultsPopup.IsOpen = true;
        }
        catch
        {
            SearchResultsPopup.IsOpen = false;
        }
    }


    private async void DoLogout()
    {
        if (SessionManager.CurrentSessionId is int sid)
        {
            var sessionRepo = Program.Services.GetRequiredService<IUserSessionRepository>();
            await sessionRepo.CloseSessionAsync(sid, DateTime.Now);
            SessionManager.CurrentSessionId = null;
        }

        SessionManager.Logout();
        _onLogout();
    }

    private void OpenSettings()
    {
        var settingsService = Program.Services.GetRequiredService<IAppSettingsService>();
        var backupService = Program.Services.GetRequiredService<IDatabaseBackupService>();
        var brandingService = Program.Services.GetRequiredService<IBrandingService>();
        var vm = new PharmacyMS.Desktop.ViewModels.SettingsViewModel(settingsService, backupService);
        var soundSettingsRepo = Program.Services.GetRequiredService<PharmacyMS.Application.Interfaces.Repositories.ISoundSettingsRepository>();
        var soundService = Program.Services.GetRequiredService<PharmacyMS.Application.Interfaces.Services.ISoundService>();
        MainContent.Content = new PharmacyMS.Desktop.Views.Settings.SettingsView(vm, brandingService, soundSettingsRepo, soundService, async () => await LoadBrandingAsync());
    }

    private Control ComingSoonPanel(string title, string message)
    {
        var border = new Border
        {
            Background = Brushes.White,
            CornerRadius = new Avalonia.CornerRadius(10),
            Padding = new Avalonia.Thickness(24),
            Margin = new Avalonia.Thickness(24),
            BoxShadow = Avalonia.Media.BoxShadows.Parse("0 1 4 0 #22000000"),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            Width = 480
        };
        var stack = new StackPanel { Spacing = 8 };
        stack.Children.Add(new TextBlock { Text = title, FontSize = 16, FontWeight = Avalonia.Media.FontWeight.Bold, Foreground = new SolidColorBrush(Color.Parse("#0F172A")) });
        stack.Children.Add(new TextBlock { Text = message, FontSize = 13, Foreground = new SolidColorBrush(Color.Parse("#64748B")), TextWrapping = Avalonia.Media.TextWrapping.Wrap });
        stack.Children.Add(new Border
        {
            Background = new SolidColorBrush(Color.Parse("#FEF3C7")),
            CornerRadius = new Avalonia.CornerRadius(6),
            Padding = new Avalonia.Thickness(10, 6),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            Child = new TextBlock { Text = "🚧 Coming soon", FontSize = 12, Foreground = new SolidColorBrush(Color.Parse("#92400E")) }
        });
        border.Child = stack;
        return border;
    }

    private void SetActive(Button active)
    {
        foreach (var btn in NavButtons)
        {
            var isActive = btn == active;
            if (isActive)
                btn.Classes.Add("active");
            else
                btn.Classes.Remove("active");
        }
    }

    private static string GetInitials(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "U";
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1) return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpperInvariant();
        return (parts[0][0].ToString() + parts[^1][0]).ToUpperInvariant();
    }

    private async Task UploadAvatarAsync()
    {
        var user = SessionManager.CurrentUser;
        if (user == null) return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Profile Photo",
            AllowMultiple = false,
            FileTypeFilter = new[] { FilePickerFileTypes.ImageAll }
        });

        if (files.Count == 0) return;

        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PharmaPro", "Avatars");
        Directory.CreateDirectory(folder);

        var ext = Path.GetExtension(files[0].Name);
        var destPath = Path.Combine(folder, $"user-{user.Id}{ext}");

        await using (var sourceStream = await files[0].OpenReadAsync())
        await using (var destStream = File.Create(destPath))
        {
            await sourceStream.CopyToAsync(destStream);
        }

        var userRepo = Program.Services.GetRequiredService<IUserRepository>();
        user.AvatarPath = destPath;
        await userRepo.UpdateAsync(user);

        AvatarImage.Source = new Avalonia.Media.Imaging.Bitmap(destPath);
        AvatarImage.IsVisible = true;
        AvatarInitialsText.IsVisible = false;
    }

    private void ShowDashboard()
    {
        var medicineRepo = Program.Services.GetRequiredService<IMedicineRepository>();
        var reportRepo = Program.Services.GetRequiredService<IReportRepository>();
        var saleRepo = Program.Services.GetRequiredService<ISaleRepository>();
        var dashExpenseRepo = Program.Services.GetRequiredService<IExpenseRepository>();
        var vm = new PharmacyMS.Desktop.ViewModels.DashboardViewModel(medicineRepo, reportRepo, saleRepo, dashExpenseRepo);
        MainContent.Content = new PharmacyMS.Desktop.Views.Dashboard.DashboardView(
            vm,
            onViewAllTopMedicines: () => NavigateToInventory(null),
            onViewAllRecentTransactions: () =>
                SalesHistoryButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Avalonia.Controls.Button.ClickEvent)));
    }

    private async Task LoadHeaderStatsAsync()
    {
        try
        {
            var medicineRepo = Program.Services.GetRequiredService<IMedicineRepository>();
            var categoryRepo = Program.Services.GetRequiredService<ICategoryRepository>();
            var supplierRepo = Program.Services.GetRequiredService<ISupplierRepository>();
            var customerRepo = Program.Services.GetRequiredService<ICustomerRepository>();
            var userRepo = Program.Services.GetRequiredService<IUserRepository>();
            var reportRepo = Program.Services.GetRequiredService<IReportRepository>();
            var saleRepo = Program.Services.GetRequiredService<ISaleRepository>();
            var statsExpenseRepo = Program.Services.GetRequiredService<IExpenseRepository>();
            var statsVm = new PharmacyMS.Desktop.ViewModels.DashboardViewModel(medicineRepo, reportRepo, saleRepo, statsExpenseRepo);
            await statsVm.LoadAsync();

            var allMedicines = (await medicineRepo.GetAllAsync()).ToList();
            var expiredMeds = allMedicines.Where(m => m.ExpiryDate.HasValue && m.ExpiryDate.Value.Date < DateTime.Today).ToList();
            var outOfStockMeds = allMedicines.Where(m => m.QuantityInStock == 0).ToList();
            var lowStockMeds = allMedicines.Where(m => m.QuantityInStock > 0 && m.QuantityInStock <= m.ReorderLevel).ToList();
            var expiringSoonMeds = allMedicines.Where(m =>
                m.ExpiryDate.HasValue &&
                m.ExpiryDate.Value.Date >= DateTime.Today &&
                m.ExpiryDate.Value.Date <= DateTime.Today.AddDays(30)).ToList();

            var expiredCount = expiredMeds.Count;
            var outOfStockCount = outOfStockMeds.Count;
            var lowStock = lowStockMeds.Count;
            var expiring = expiringSoonMeds.Count;

            var allCustomers = (await customerRepo.GetAllAsync()).ToList();
            var newCustomersToday = allCustomers.Count(c => c.CreatedAt.Date == DateTime.Today);

            var total = expiredCount + outOfStockCount + lowStock + expiring;

            // --- Detect newly-appeared alerts and show toasts (skip on first load after app start) ---
            var currentKeys = new HashSet<string>();
            foreach (var m in expiredMeds) currentKeys.Add($"expired:{m.Id}");
            foreach (var m in outOfStockMeds) currentKeys.Add($"outofstock:{m.Id}");
            foreach (var m in lowStockMeds) currentKeys.Add($"lowstock:{m.Id}");
            foreach (var m in expiringSoonMeds) currentKeys.Add($"expiring:{m.Id}");

            if (!_firstStatsLoad)
            {
                var newKeys = currentKeys.Except(_seenAlertKeys).ToList();
                foreach (var key in newKeys)
                {
                    var parts = key.Split(':');
                    var kind = parts[0];
                    var id = int.Parse(parts[1]);
                    var med = allMedicines.FirstOrDefault(m => m.Id == id);
                    if (med == null) continue;

                    var (icon, label, color) = kind switch
                    {
                        "expired" => ("⚠", $"{med.Name} has expired", "#EF4444"),
                        "outofstock" => ("⚠", $"{med.Name} is out of stock", "#EF4444"),
                        "lowstock" => ("📦", $"{med.Name} is low on stock", "#F59E0B"),
                        "expiring" => ("⏳", $"{med.Name} expires soon", "#F59E0B"),
                        _ => ("🔔", med.Name, "#64748B")
                    };
                    ShowToast(icon, label, color);
                }
            }
            _seenAlertKeys = currentKeys;
            _firstStatsLoad = false;

            NotifCountText.Text = total.ToString();
            NotifBadge.IsVisible = total > 0;
            NotifHeaderText.Text = $"🔔 Notifications ({total})";

            // Pending receiving badge (Purchase Orders awaiting receipt)
            var orderRepoForBadge = Program.Services.GetRequiredService<PharmacyMS.Application.Interfaces.Repositories.IPurchaseOrderRepository>();
            var pendingReceivingCount = (await orderRepoForBadge.GetPendingReceivingAsync()).Count();
            PendingReceivingBadgeText.Text = pendingReceivingCount.ToString();
            PendingReceivingBadge.IsVisible = pendingReceivingCount > 0;

            // Approvals badge (Admin only)
            if (SessionManager.IsAdmin)
            {
                var purchaseRepo = Program.Services.GetRequiredService<IPurchaseRepository>();
                var pendingCustomers = (await customerRepo.GetAllAsync()).Count(x => x.ApprovalStatus == PharmacyMS.Domain.Enums.ApprovalStatus.Pending);
                var pendingSuppliers = (await supplierRepo.GetAllAsync()).Count(x => x.ApprovalStatus == PharmacyMS.Domain.Enums.ApprovalStatus.Pending);
                var pendingPurchases = (await purchaseRepo.GetAllAsync()).Count(x => x.ApprovalStatus == PharmacyMS.Domain.Enums.ApprovalStatus.Pending);
                var goodsReceiptRepoForBadge = Program.Services.GetRequiredService<PharmacyMS.Application.Interfaces.Repositories.IGoodsReceiptRepository>();
                var pendingReceiptsForBadge = (await goodsReceiptRepoForBadge.GetAllAsync()).Count(x => x.ApprovalStatus == PharmacyMS.Domain.Enums.ApprovalStatus.Pending);
                var paymentRepoForBadge = Program.Services.GetRequiredService<PharmacyMS.Application.Interfaces.Repositories.IPendingSalePaymentRepository>();
                var pendingPaymentsForBadge = (await paymentRepoForBadge.GetPendingAsync()).Count;
                var expenseRepoForBadge = Program.Services.GetRequiredService<PharmacyMS.Application.Interfaces.Repositories.IPendingExpenseRepository>();
                var pendingExpensesForBadge = (await expenseRepoForBadge.GetPendingAsync()).Count;
                var pendingTotal = pendingCustomers + pendingSuppliers + pendingPurchases + pendingReceiptsForBadge + pendingPaymentsForBadge + pendingExpensesForBadge;
                ApprovalsBadgeText.Text = pendingTotal.ToString();
                ApprovalsBadge.IsVisible = pendingTotal > 0;
            }

            NotifExpiredText.Text = $"⚠ {expiredCount} medicines have expired";
            NotifExpiredButton.IsVisible = expiredCount > 0;

            NotifOutOfStockText.Text = $"⚠ {outOfStockCount} medicines are out of stock";
            NotifOutOfStockButton.IsVisible = outOfStockCount > 0;

            NotifLowStockText.Text = $"📦 {lowStock} medicines are low stock";
            NotifLowStockButton.IsVisible = lowStock > 0;

            NotifExpiringText.Text = $"⏳ {expiring} medicines expire within 30 days";
            NotifExpiringButton.IsVisible = expiring > 0;

            NotifRevenueText.Text = $"💰 Today's sales: ${statsVm.TodayRevenue:F2}";

            NotifNewCustomersText.Text = $"👥 {newCustomersToday} new customer{(newCustomersToday == 1 ? "" : "s")} today";
            NotifNewCustomersButton.IsVisible = newCustomersToday > 0;

            NotifCriticalSection.IsVisible = expiredCount > 0 || outOfStockCount > 0;
            NotifWarningSection.IsVisible = lowStock > 0 || expiring > 0;

            NotifEmptyText.IsVisible = total == 0;

            NotifExpiredButton.Click += (_, _) => NavigateToInventory(StockStatus.Expired);
            NotifOutOfStockButton.Click += (_, _) => NavigateToInventory(StockStatus.OutOfStock);
            NotifLowStockButton.Click += (_, _) => NavigateToInventory(StockStatus.LowStock);
            NotifExpiringButton.Click += (_, _) => NavigateToInventory(null, expiringOnly: true);
            NotifNewCustomersButton.Click += (_, _) =>
            {
                SetActive(CustomersButton);
                PageTitleText.Text = "Customers";
                var repo = Program.Services.GetRequiredService<ICustomerRepository>();
                var vm = new PharmacyMS.Desktop.ViewModels.CustomersViewModel(repo);
                MainContent.Content = new PharmacyMS.Desktop.Views.Customers.CustomersView(vm);
            };
        }
        catch
        {
            // header stats are non-critical; fail silently so the app still opens
        }
    }

    private void ShowToast(string icon, string message, string accentColor)
    {
        var border = new Border
        {
            Background = Brushes.White,
            CornerRadius = new Avalonia.CornerRadius(10),
            Padding = new Avalonia.Thickness(14, 12),
            BoxShadow = Avalonia.Media.BoxShadows.Parse("0 4 16 0 #33000000"),
            BorderBrush = new SolidColorBrush(Color.Parse(accentColor)),
            BorderThickness = new Avalonia.Thickness(1, 0, 0, 0)
        };

        var stack = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 10 };
        stack.Children.Add(new TextBlock { Text = icon, FontSize = 16, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
        stack.Children.Add(new TextBlock
        {
            Text = message,
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.Parse("#0F172A")),
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Width = 260
        });
        border.Child = stack;

        border.Transitions = new Avalonia.Animation.Transitions
        {
            new Avalonia.Animation.DoubleTransition { Property = OpacityProperty, Duration = TimeSpan.FromMilliseconds(250) }
        };
        border.Opacity = 0;

        ToastHost.Children.Add(border);
        border.Opacity = 1;

        var dismissTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(6) };
        dismissTimer.Tick += (_, _) =>
        {
            dismissTimer.Stop();
            border.Opacity = 0;
            var removeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            removeTimer.Tick += (_, _) =>
            {
                removeTimer.Stop();
                ToastHost.Children.Remove(border);
            };
            removeTimer.Start();
        };
        dismissTimer.Start();
    }

    private void NavigateToInventory(StockStatus? status, bool expiringOnly = false)
    {
        SetActive(InventoryButton);
        PageTitleText.Text = "Inventory";
        var repo = Program.Services.GetRequiredService<IMedicineRepository>();
        var vm = new InventoryViewModel(repo);
        var view = new PharmacyMS.Desktop.Views.Inventory.InventoryView(vm);
        MainContent.Content = view;

        if (status.HasValue)
            view.ApplyStatusFilter(status.Value);
        else if (expiringOnly)
            view.ApplyExpiringFilter();
    }

    private async Task LoadBrandingAsync()
    {
        var brandingService = Program.Services.GetRequiredService<IBrandingService>();
        var branding = await brandingService.GetAsync();

        AppNameText.Text = branding.AppName;

        if (!string.IsNullOrWhiteSpace(branding.LogoPath) && File.Exists(branding.LogoPath))
        {
            LogoImage.Source = new Avalonia.Media.Imaging.Bitmap(branding.LogoPath);
            LogoImage.IsVisible = true;
            DefaultLogoBorder.IsVisible = false;
        }
    }
}