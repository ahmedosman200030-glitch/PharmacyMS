using System.Globalization;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Desktop.Views.Inventory;

public partial class MedicineFormWindow : Window
{
    private readonly Medicine _medicine;

    public MedicineFormWindow(Medicine? existing = null)
    {
        InitializeComponent();
        _medicine = existing ?? new Medicine();

        HeaderText.Text = existing != null ? "Edit Medicine" : "Add Medicine";

        var units = new List<string>
        {
            "Box", "Bottle", "Packet", "Pcs", "Vial", "Ampoule",
            "Sachet", "Strip", "Tube", "Syringe", "Roll", "Other"
        };
        UnitBox.ItemsSource = units;
        UnitBox.SelectedItem = existing?.Unit ?? "Box";

        Opened += async (_, _) =>
        {
            var categoryRepo = Program.Services.GetRequiredService<ICategoryRepository>();
            var categories = await categoryRepo.GetAllAsync();
            CategoryBox.ItemsSource = categories
                .Where(c => c.IsActive)
                .Select(c => c.Name)
                .OrderBy(n => n)
                .ToList();

            var supplierRepo = Program.Services.GetRequiredService<ISupplierRepository>();
            var suppliers = await supplierRepo.GetAllAsync();
            SupplierBox.ItemsSource = suppliers
                .Select(s => s.Name)
                .OrderBy(n => n)
                .ToList();

            if (existing != null)
            {
                CategoryBox.Text = existing.Category;
                SupplierBox.Text = existing.Supplier;
            }
        };

        if (existing != null)
        {
            NameBox.Text = existing.Name;
            GenericNameBox.Text = existing.GenericName;
            ManufacturerBox.Text = existing.Manufacturer;
            UnitPriceBox.Text = existing.UnitPrice.ToString(CultureInfo.InvariantCulture);
            CostPriceBox.Text = existing.CostPrice.ToString(CultureInfo.InvariantCulture);
            QuantityBox.Text = existing.QuantityInStock.ToString();
            ReorderLevelBox.Text = existing.ReorderLevel.ToString();
            BatchNumberBox.Text = existing.BatchNumber;
            ExpiryDateBox.Text = existing.ExpiryDate?.ToString("yyyy-MM-dd");
            UnitBox.SelectedItem = existing.Unit ?? "Box";
        }

        SaveButton.Click += (_, _) => Save();
        CancelButton.Click += (_, _) => Close(null);
    }

    private void Save()
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text)) { ShowError("Name is required."); return; }

        if (!decimal.TryParse(UnitPriceBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
        { ShowError("Unit price must be a number."); return; }

        decimal.TryParse(CostPriceBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var costPrice);

        if (!int.TryParse(QuantityBox.Text, out var qty)) { ShowError("Quantity must be a whole number."); return; }

        if (!int.TryParse(ReorderLevelBox.Text, out var reorder)) reorder = 10;

        DateTime? expiry = null;
        if (!string.IsNullOrWhiteSpace(ExpiryDateBox.Text))
        {
            if (!DateTime.TryParse(ExpiryDateBox.Text, out var parsedDate))
            { ShowError("Expiry date must be in yyyy-MM-dd format."); return; }
            expiry = parsedDate;
        }

        _medicine.Name = NameBox.Text!.Trim();
        _medicine.GenericName = GenericNameBox.Text?.Trim();
        _medicine.Category = (CategoryBox.SelectedItem as string ?? CategoryBox.Text)?.Trim();
        _medicine.Manufacturer = ManufacturerBox.Text?.Trim();
        _medicine.Supplier = (SupplierBox.SelectedItem as string ?? SupplierBox.Text)?.Trim();
        _medicine.UnitPrice = price;
        _medicine.CostPrice = costPrice;
        _medicine.QuantityInStock = qty;
        _medicine.ReorderLevel = reorder;
        _medicine.BatchNumber = BatchNumberBox.Text?.Trim();
        _medicine.ExpiryDate = expiry;
        _medicine.Unit = (UnitBox.SelectedItem as string) ?? "Box";

        Close(_medicine);
    }

    private void ShowError(string message) { ErrorText.Text = message; ErrorText.IsVisible = true; }
}
