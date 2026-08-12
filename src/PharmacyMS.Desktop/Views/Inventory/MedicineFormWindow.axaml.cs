using System.Globalization;
using Avalonia.Controls;
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

        if (existing != null)
        {
            NameBox.Text = existing.Name;
            GenericNameBox.Text = existing.GenericName;
            CategoryBox.Text = existing.Category;
            ManufacturerBox.Text = existing.Manufacturer;
            SupplierBox.Text = existing.Supplier;
            UnitPriceBox.Text = existing.UnitPrice.ToString(CultureInfo.InvariantCulture);
            CostPriceBox.Text = existing.CostPrice.ToString(CultureInfo.InvariantCulture);
            QuantityBox.Text = existing.QuantityInStock.ToString();
            ReorderLevelBox.Text = existing.ReorderLevel.ToString();
            BatchNumberBox.Text = existing.BatchNumber;
            ExpiryDateBox.Text = existing.ExpiryDate?.ToString("yyyy-MM-dd");
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
        _medicine.Category = CategoryBox.Text?.Trim();
        _medicine.Manufacturer = ManufacturerBox.Text?.Trim();
        _medicine.Supplier = SupplierBox.Text?.Trim();
        _medicine.UnitPrice = price;
        _medicine.CostPrice = costPrice;
        _medicine.QuantityInStock = qty;
        _medicine.ReorderLevel = reorder;
        _medicine.BatchNumber = BatchNumberBox.Text?.Trim();
        _medicine.ExpiryDate = expiry;

        Close(_medicine);
    }

    private void ShowError(string message) { ErrorText.Text = message; ErrorText.IsVisible = true; }
}
