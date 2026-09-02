import re, sys

def apply(path, replacements):
    with open(path, encoding="utf-8") as f:
        content = f.read()
    for old, new, label in replacements:
        count = content.count(old)
        if count != 1:
            print(f"FAILED [{label}] in {path}: found {count} occurrences (expected 1)")
            sys.exit(1)
        content = content.replace(old, new, 1)
        print(f"OK [{label}]")
    with open(path, "w", encoding="utf-8") as f:
        f.write(content)

XAML = "src/PharmacyMS.Desktop/Views/Shell/MainShell.axaml"
CS = "src/PharmacyMS.Desktop/Views/Shell/MainShell.axaml.cs"

# --- 1. XAML: insert New Supplier row before the Backup row ---
apply(XAML, [
(
'''                          <Border x:Name="NotifBackupRow" Classes="notifRowStatic" Padding="10,10" CornerRadius="9" IsVisible="False">''',
'''                          <Button x:Name="NotifNewSupplierButton" Classes="notifRowModern" HorizontalAlignment="Stretch" IsVisible="False">
                            <Grid ColumnDefinitions="Auto,*,Auto">
                              <Border Grid.Column="0" Width="36" Height="36" CornerRadius="9" Background="#DBEAFE" VerticalAlignment="Top">
                                <TextBlock Text="\U0001F3E2" FontSize="15" HorizontalAlignment="Center" VerticalAlignment="Center"/>
                              </Border>
                              <StackPanel Grid.Column="1" Spacing="1" Margin="12,0,8,0" VerticalAlignment="Center">
                                <TextBlock Text="New Supplier" FontSize="13" FontWeight="SemiBold" Foreground="#0F172A"/>
                                <TextBlock x:Name="NotifNewSupplierText" Text="0 new suppliers added today" FontSize="11.5" Foreground="#64748B" TextWrapping="Wrap"/>
                              </StackPanel>
                              <StackPanel Grid.Column="2" Spacing="4" HorizontalAlignment="Right">
                                <TextBlock x:Name="NotifNewSupplierTimeText" Text="" FontSize="10" Foreground="#94A3B8" HorizontalAlignment="Right"/>
                                <Border Background="#3B82F6" CornerRadius="10" Padding="7,2" HorizontalAlignment="Right">
                                  <TextBlock x:Name="NotifNewSupplierCountBadge" Text="0" FontSize="10.5" FontWeight="Bold" Foreground="White"/>
                                </Border>
                              </StackPanel>
                            </Grid>
                          </Button>

                          <Border x:Name="NotifBackupRow" Classes="notifRowStatic" Padding="10,10" CornerRadius="9" IsVisible="False">''',
"insert New Supplier XAML row"
),
])

# --- 2. Code-behind: compute mostRecentCustomer ---
apply(CS, [
(
'''            var allCustomers = (await customerRepo.GetAllAsync()).ToList();
            var newCustomersToday = allCustomers.Count(c => c.CreatedAt.Date == DateTime.Today);''',
'''            var allCustomers = (await customerRepo.GetAllAsync()).ToList();
            var newCustomersList = allCustomers.Where(c => c.CreatedAt.Date == DateTime.Today).ToList();
            var newCustomersToday = newCustomersList.Count;
            var mostRecentCustomer = newCustomersList.OrderByDescending(c => c.CreatedAt).FirstOrDefault();''',
"compute mostRecentCustomer"
),
])

# --- 3. Code-behind: compute mostRecentSale and supplier stats ---
apply(CS, [
(
'''            var newOrdersCount = newOrdersToday.Count;
            var mostRecentOrder = newOrdersToday.OrderByDescending(o => o.CreatedAt).FirstOrDefault();''',
'''            var newOrdersCount = newOrdersToday.Count;
            var mostRecentOrder = newOrdersToday.OrderByDescending(o => o.CreatedAt).FirstOrDefault();

            var allSalesForNotif = (await saleRepo.GetAllAsync()).ToList();
            var todaySalesForNotif = allSalesForNotif.Where(s => s.CreatedAt.Date == DateTime.Today).ToList();
            var mostRecentSale = todaySalesForNotif.OrderByDescending(s => s.CreatedAt).FirstOrDefault();

            var notifSupplierRepo = Program.Services.GetRequiredService<ISupplierRepository>();
            var allSuppliersForNotif = (await notifSupplierRepo.GetAllAsync()).ToList();
            var newSuppliersToday = allSuppliersForNotif.Where(s => s.CreatedAt.Date == DateTime.Today).ToList();
            var newSupplierCount = newSuppliersToday.Count;
            var mostRecentSupplier = newSuppliersToday.OrderByDescending(s => s.CreatedAt).FirstOrDefault();''',
"compute mostRecentSale and supplier stats"
),
])

# --- 4. Code-behind: wire timestamps + new supplier row into the population block ---
apply(CS, [
(
'''            NotifRevenueText.Text = $"💰 Today's sales: ${statsVm.TodayRevenue:F2}";''',
'''            NotifRevenueText.Text = $"💰 Today's sales: ${statsVm.TodayRevenue:F2}";
            NotifRevenueTimeText.Text = mostRecentSale != null ? GetTimeAgo(mostRecentSale.CreatedAt) : "";''',
"add NotifRevenueTimeText"
),
(
'''            NotifNewCustomersTimeText.Text = "";''',
'''            NotifNewCustomersTimeText.Text = mostRecentCustomer != null ? GetTimeAgo(mostRecentCustomer.CreatedAt) : "";''',
"wire NotifNewCustomersTimeText"
),
(
'''            NotifBackupRow.IsVisible = backupReminderApplicable && backupDue;''',
'''            NotifNewSupplierText.Text = $"🏢 {newSupplierCount} new supplier{(newSupplierCount == 1 ? "" : "s")} added today";
            NotifNewSupplierButton.IsVisible = newSupplierCount > 0;
            NotifNewSupplierCountBadge.Text = newSupplierCount.ToString();
            NotifNewSupplierTimeText.Text = mostRecentSupplier != null ? GetTimeAgo(mostRecentSupplier.CreatedAt) : "";

            NotifBackupRow.IsVisible = backupReminderApplicable && backupDue;''',
"populate New Supplier row"
),
])

# --- 5. Code-behind: wire the New Supplier row's click handler ---
apply(CS, [
(
'''        NotifNewPurchaseButton.Click += (_, _) =>
        {
            var repo = Program.Services.GetRequiredService<PharmacyMS.Application.Interfaces.Repositories.IPurchaseOrderRepository>();
            var vm = new PharmacyMS.Desktop.ViewModels.PurchaseOrderListViewModel(repo);
            MainContent.Content = new PharmacyMS.Desktop.Views.Purchases.PurchaseOrderListView(vm);
        };
    }''',
'''        NotifNewPurchaseButton.Click += (_, _) =>
        {
            var repo = Program.Services.GetRequiredService<PharmacyMS.Application.Interfaces.Repositories.IPurchaseOrderRepository>();
            var vm = new PharmacyMS.Desktop.ViewModels.PurchaseOrderListViewModel(repo);
            MainContent.Content = new PharmacyMS.Desktop.Views.Purchases.PurchaseOrderListView(vm);
        };
        NotifNewSupplierButton.Click += (_, _) =>
        {
            SetActive(SuppliersButton);
            PageTitleText.Text = "Suppliers";
            var supplierRepoForClick = Program.Services.GetRequiredService<ISupplierRepository>();
            var supplierVmForClick = new PharmacyMS.Desktop.ViewModels.SuppliersViewModel(supplierRepoForClick);
            MainContent.Content = new PharmacyMS.Desktop.Views.Suppliers.SuppliersView(supplierVmForClick);
        };
    }''',
"wire New Supplier click handler"
),
])

print("All patches applied successfully.")
