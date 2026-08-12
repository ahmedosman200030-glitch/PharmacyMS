using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using PharmacyMS.Desktop.ViewModels;
using PharmacyMS.Desktop.Views.Purchases;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Desktop.Views.Inventory;

public partial class InventoryView : UserControl
{
    private readonly InventoryViewModel _vm;
    private StockStatus? _activeStatusFilter = null;
    private bool _isLoaded = false;
    private Action? _pendingAction = null;

    public InventoryView() { InitializeComponent(); }
    public InventoryView(InventoryViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        Grid.ItemsSource = _vm.Rows;

        AttachedToVisualTree += async (_, _) =>
        {
            await _vm.LoadAsync();
            RefreshFilterCombos();
            RefreshSummary();
            _isLoaded = true;
            _pendingAction?.Invoke();
            _pendingAction = null;
        };

        SearchBox.PropertyChanged += (_, e) =>
        {
            if (e.Property == TextBox.TextProperty) RunFilter();
        };
        CategoryCombo.SelectionChanged += (_, _) => RunFilter();
        SupplierCombo.SelectionChanged += (_, _) => RunFilter();

        ClearFiltersButton.Click += (_, _) =>
        {
            SearchBox.Text = "";
            CategoryCombo.SelectedIndex = 0;
            SupplierCombo.SelectedIndex = 0;
            _activeStatusFilter = null;
            RunFilter();
        };

        LowStockChip.Click += (_, _) => ToggleStatusFilter(StockStatus.LowStock);
        OutOfStockChip.Click += (_, _) => ToggleStatusFilter(StockStatus.OutOfStock);
        ExpiringChip.Click += (_, _) =>
        {
            // "Expiring" is a date-window filter, not a single status — clear status filter and rely on search-less list,
            // then narrow to rows whose ExpiryDisplay indicates days-left. Simplest: filter client-side here.
            _activeStatusFilter = null;
            RunFilter();
            var expiringRows = _vm.Rows.Where(r =>
                r.ExpiryDate.HasValue &&
                r.ExpiryDate.Value.Date >= DateTime.Today &&
                r.ExpiryDate.Value.Date <= DateTime.Today.AddDays(30)).ToList();
            Grid.ItemsSource = null;
            Grid.ItemsSource = expiringRows;
        };

        Grid.DoubleTapped += async (_, _) => await EditSelectedAsync();
        EditButton.Click += async (_, _) => await EditSelectedAsync();

        AddButton.Click += async (_, _) =>
        {
            var form = new MedicineFormWindow();
            var result = await form.ShowDialog<Medicine?>(TopLevel.GetTopLevel(this) as Window);
            if (result != null)
            {
                await _vm.AddAsync(result);
                RefreshFilterCombos();
                RefreshSummary();
            }
        };

        DeleteButton.Click += async (_, _) =>
        {
            if (Grid.SelectedItem is MedicineRow row)
            {
                await _vm.DeleteAsync(row.Medicine);
                RefreshSummary();
            }
        };

        RestockButton.Click += (_, _) =>
        {
            if (Grid.SelectedItem is not MedicineRow row) return;
            var win = new PurchaseWindow(new PharmacyMS.Desktop.ViewModels.PurchaseViewModel(
                Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<PharmacyMS.Application.Interfaces.Repositories.IMedicineRepository>(PharmacyMS.Desktop.Program.Services),
                Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<PharmacyMS.Application.Interfaces.Repositories.IPurchaseRepository>(PharmacyMS.Desktop.Program.Services),
                Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<PharmacyMS.Application.Interfaces.Repositories.ISupplierRepository>(PharmacyMS.Desktop.Program.Services)
            ));
            win.Show();
        };

        AdjustStockButton.Click += (_, _) =>
        {
            var vm2 = new PharmacyMS.Desktop.ViewModels.StockAdjustmentViewModel(
                Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<PharmacyMS.Application.Interfaces.Repositories.IMedicineRepository>(PharmacyMS.Desktop.Program.Services),
                Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<PharmacyMS.Application.Interfaces.Repositories.IStockAdjustmentRepository>(PharmacyMS.Desktop.Program.Services)
            );
            var win = new StockAdjustmentWindow(vm2);
            win.Show();
        };

        ExportButton.Click += async (_, _) => await ExportCsvAsync();
    }

    private async Task EditSelectedAsync()
    {
        if (Grid.SelectedItem is not MedicineRow row) return;
        var form = new MedicineFormWindow(row.Medicine);
        var result = await form.ShowDialog<Medicine?>(TopLevel.GetTopLevel(this) as Window);
        if (result != null)
        {
            await _vm.UpdateAsync(result);
            RefreshFilterCombos();
            RefreshSummary();
        }
    }

    private void RefreshFilterCombos()
    {
        CategoryCombo.ItemsSource = _vm.Categories;
        CategoryCombo.SelectedIndex = 0;
        SupplierCombo.ItemsSource = _vm.Suppliers;
        SupplierCombo.SelectedIndex = 0;
    }

    private void ToggleStatusFilter(StockStatus status)
    {
        _activeStatusFilter = _activeStatusFilter == status ? null : status;
        RunFilter();
    }

    public void ApplyStatusFilter(StockStatus status)
    {
        if (!_isLoaded) { _pendingAction = () => ApplyStatusFilter(status); return; }
        _activeStatusFilter = status;
        RunFilter();
    }

    public void ApplyExpiringFilter()
    {
        if (!_isLoaded) { _pendingAction = ApplyExpiringFilter; return; }
        _activeStatusFilter = null;
        RunFilter();
        var expiringRows = _vm.Rows.Where(r =>
            r.ExpiryDate.HasValue &&
            r.ExpiryDate.Value.Date >= DateTime.Today &&
            r.ExpiryDate.Value.Date <= DateTime.Today.AddDays(30)).ToList();
        Grid.ItemsSource = null;
        Grid.ItemsSource = expiringRows;
    }

    private void RunFilter()
    {
        var category = CategoryCombo.SelectedItem as string;
        var supplier = SupplierCombo.SelectedItem as string;
        _vm.ApplyFilter(SearchBox.Text, category, supplier, _activeStatusFilter);
        Grid.ItemsSource = _vm.Rows;
    }

    private void RefreshSummary()
    {
        TotalSkusText.Text = _vm.TotalSKUs.ToString();
        StockValueText.Text = $"${_vm.TotalCostValue:N2}";
        RetailValueText.Text = $"${_vm.TotalRetailValue:N2}";
        LowStockCountText.Text = _vm.LowStockCount.ToString();
        OutOfStockCountText.Text = _vm.OutOfStockCount.ToString();
        ExpiringCountText.Text = _vm.ExpiringCount.ToString();
    }

    private async Task ExportCsvAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Inventory",
            SuggestedFileName = $"inventory-{DateTime.Now:yyyyMMdd}.csv",
            FileTypeChoices = new[] { new FilePickerFileType("CSV") { Patterns = new[] { "*.csv" } } }
        });

        if (file == null) return;

        var csv = _vm.ExportToCsv();
        await using var stream = await file.OpenWriteAsync();
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(csv);
    }
}
