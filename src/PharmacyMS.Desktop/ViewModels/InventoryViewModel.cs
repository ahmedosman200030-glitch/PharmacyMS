using System.Collections.ObjectModel;
using ClosedXML.Excel;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Desktop.ViewModels;

public enum StockStatus { Healthy, LowStock, OutOfStock, Expired }

public class MedicineRow
{
    public Medicine Medicine { get; set; } = null!;
    public int Id => Medicine.Id;
    public string Name => Medicine.Name;
    public string GenericName => Medicine.GenericName ?? "";
    public string Category => Medicine.Category ?? "Uncategorized";
    public string Supplier => Medicine.Supplier ?? "";
    public decimal UnitPrice => Medicine.UnitPrice;
    public decimal CostPrice => Medicine.CostPrice;
    public decimal MarginPercent => CostPrice > 0
        ? Math.Round((UnitPrice - CostPrice) / UnitPrice * 100, 1) : 0;
    public int QuantityInStock => Medicine.QuantityInStock;
    public int ReorderLevel => Medicine.ReorderLevel;
    public string BatchNumber => Medicine.BatchNumber ?? "";
    public DateTime? ExpiryDate => Medicine.ExpiryDate;

    public string ExpiryDisplay
    {
        get
        {
            if (!ExpiryDate.HasValue) return "—";
            var days = (ExpiryDate.Value.Date - DateTime.Today).Days;
            if (days < 0) return $"Expired {-days}d ago";
            if (days == 0) return "Expires today";
            if (days <= 60) return $"{days}d left";
            return ExpiryDate.Value.ToString("dd MMM yyyy");
        }
    }

    public StockStatus Status
    {
        get
        {
            if (ExpiryDate.HasValue && ExpiryDate.Value.Date < DateTime.Today) return StockStatus.Expired;
            if (QuantityInStock == 0) return StockStatus.OutOfStock;
            if (QuantityInStock <= ReorderLevel) return StockStatus.LowStock;
            return StockStatus.Healthy;
        }
    }

    public string StatusLabel => Status switch
    {
        StockStatus.Healthy => "Healthy",
        StockStatus.LowStock => "Low Stock",
        StockStatus.OutOfStock => "Out of Stock",
        StockStatus.Expired => "Expired",
        _ => ""
    };

    public string StatusColor => Status switch
    {
        StockStatus.Healthy => "#22C55E",
        StockStatus.LowStock => "#F59E0B",
        StockStatus.OutOfStock => "#EF4444",
        StockStatus.Expired => "#6B7280",
        _ => "#E2E8F0"
    };

    public string RowBackground => Status switch
    {
        StockStatus.Expired => "#FFF1F2",
        StockStatus.OutOfStock => "#FFF1F2",
        StockStatus.LowStock => "#FFFBEB",
        _ => "White"
    };

    public string ExpiryColor => Status switch
    {
        StockStatus.Expired => "#EF4444",
        StockStatus.OutOfStock => "#EF4444",
        StockStatus.LowStock => "#F59E0B",
        _ => "#334155"
    };
}

public class ExcelImportResult
{
    public int Added { get; set; }
    public int Skipped { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class InventoryViewModel
{
    private readonly IMedicineRepository _repository;
    private List<MedicineRow> _allRows = new();

    public ObservableCollection<MedicineRow> Rows { get; } = new();
    public ObservableCollection<string> Categories { get; } = new();
    public ObservableCollection<string> Suppliers { get; } = new();

    public int TotalSKUs { get; private set; }
    public int OutOfStockCount { get; private set; }
    public int LowStockCount { get; private set; }
    public int ExpiringCount { get; private set; }
    public int ExpiredCount { get; private set; }
    public decimal TotalCostValue { get; private set; }
    public decimal TotalRetailValue { get; private set; }

    public ObservableCollection<Medicine> Medicines { get; } = new();

    public InventoryViewModel(IMedicineRepository repository)
    {
        _repository = repository;
    }

    public async Task LoadAsync()
    {
        Medicines.Clear();
        _allRows.Clear();

        var items = (await _repository.GetAllAsync()).ToList();
        foreach (var m in items)
        {
            Medicines.Add(m);
            _allRows.Add(new MedicineRow { Medicine = m });
        }

        Categories.Clear();
        Categories.Add("All Categories");
        foreach (var c in items
            .Select(m => string.IsNullOrWhiteSpace(m.Category) ? "Uncategorized" : m.Category)
            .Distinct().OrderBy(x => x))
            Categories.Add(c);

        Suppliers.Clear();
        Suppliers.Add("All Suppliers");
        foreach (var s in items
            .Where(m => !string.IsNullOrWhiteSpace(m.Supplier))
            .Select(m => m.Supplier!)
            .Distinct().OrderBy(x => x))
            Suppliers.Add(s);

        TotalSKUs = _allRows.Count;
        OutOfStockCount = _allRows.Count(r => r.Status == StockStatus.OutOfStock);
        LowStockCount = _allRows.Count(r => r.Status == StockStatus.LowStock);
        ExpiringCount = _allRows.Count(r =>
            r.ExpiryDate.HasValue &&
            r.ExpiryDate.Value.Date >= DateTime.Today &&
            r.ExpiryDate.Value.Date <= DateTime.Today.AddDays(30));
        ExpiredCount = _allRows.Count(r => r.Status == StockStatus.Expired);
        TotalCostValue = _allRows.Sum(r => r.CostPrice * r.QuantityInStock);
        TotalRetailValue = _allRows.Sum(r => r.UnitPrice * r.QuantityInStock);

        ApplyFilter(null, null, null, null);
    }

    public void ApplyFilter(string? search, string? category, string? supplier, StockStatus? statusFilter)
    {
        Rows.Clear();
        var query = _allRows.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            query = query.Where(r =>
                r.Name.ToLowerInvariant().Contains(s) ||
                r.GenericName.ToLowerInvariant().Contains(s) ||
                r.BatchNumber.ToLowerInvariant().Contains(s) ||
                r.Category.ToLowerInvariant().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(category) && category != "All Categories")
            query = query.Where(r => r.Category == category);

        if (!string.IsNullOrWhiteSpace(supplier) && supplier != "All Suppliers")
            query = query.Where(r => r.Supplier == supplier);

        if (statusFilter.HasValue)
            query = query.Where(r => r.Status == statusFilter.Value);

        foreach (var row in query.OrderBy(r => r.Name))
            Rows.Add(row);
    }

    public string ExportToCsv()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Name,Generic Name,Category,Supplier,Unit Price,Cost Price,Margin %,Stock,Reorder Level,Batch,Expiry,Status");
        foreach (var r in _allRows.OrderBy(x => x.Name))
        {
            sb.AppendLine($"\"{r.Name}\",\"{r.GenericName}\",\"{r.Category}\",\"{r.Supplier}\"," +
                          $"{r.UnitPrice},{r.CostPrice},{r.MarginPercent}," +
                          $"{r.QuantityInStock},{r.ReorderLevel}," +
                          $"\"{r.BatchNumber}\",\"{r.ExpiryDate?.ToString("yyyy-MM-dd")}\",\"{r.StatusLabel}\"");
        }
        return sb.ToString();
    }

    public XLWorkbook ExportToExcelWorkbook()
    {
        var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Inventory");

        var headers = new[] { "Name", "Generic Name", "Category", "Supplier", "Unit Price", "Cost Price",
            "Margin %", "Stock", "Reorder Level", "Batch", "Expiry", "Status" };
        for (int i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];
        ws.Row(1).Style.Font.Bold = true;
        ws.Row(1).Style.Fill.BackgroundColor = XLColor.FromArgb(15, 42, 67);
        ws.Row(1).Style.Font.FontColor = XLColor.White;

        var row = 2;
        foreach (var r in _allRows.OrderBy(x => x.Name))
        {
            ws.Cell(row, 1).Value = r.Name;
            ws.Cell(row, 2).Value = r.GenericName;
            ws.Cell(row, 3).Value = r.Category;
            ws.Cell(row, 4).Value = r.Supplier;
            ws.Cell(row, 5).Value = r.UnitPrice;
            ws.Cell(row, 6).Value = r.CostPrice;
            ws.Cell(row, 7).Value = r.MarginPercent;
            ws.Cell(row, 8).Value = r.QuantityInStock;
            ws.Cell(row, 9).Value = r.ReorderLevel;
            ws.Cell(row, 10).Value = r.BatchNumber;
            ws.Cell(row, 11).Value = r.ExpiryDate?.ToString("yyyy-MM-dd") ?? "";
            ws.Cell(row, 12).Value = r.StatusLabel;

            var statusColor = r.Status switch
            {
                StockStatus.OutOfStock => XLColor.FromArgb(254, 226, 226),
                StockStatus.Expired => XLColor.FromArgb(254, 226, 226),
                StockStatus.LowStock => XLColor.FromArgb(254, 243, 199),
                _ => XLColor.White
            };
            ws.Range(row, 1, row, 12).Style.Fill.BackgroundColor = statusColor;
            row++;
        }

        ws.Columns().AdjustToContents();
        return wb;
    }

    public async Task<ExcelImportResult> ImportFromExcelAsync(Stream fileStream)
    {
        var result = new ExcelImportResult();

        using var ms = new MemoryStream();
        await fileStream.CopyToAsync(ms);
        ms.Position = 0;

        using var wb = new XLWorkbook(ms);
        var ws = wb.Worksheets.First();

        var headerRow = ws.Row(1);
        var colMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in headerRow.CellsUsed())
            colMap[cell.GetString().Trim()] = cell.Address.ColumnNumber;

        int Col(params string[] names)
        {
            foreach (var n in names)
                if (colMap.TryGetValue(n, out var c)) return c;
            return -1;
        }

        var nameCol = Col("Name");
        var genericCol = Col("Generic Name", "GenericName");
        var categoryCol = Col("Category");
        var supplierCol = Col("Supplier");
        var manufacturerCol = Col("Manufacturer");
        var priceCol = Col("Unit Price", "Price", "UnitPrice");
        var costCol = Col("Cost Price", "CostPrice", "Cost");
        var stockCol = Col("Stock", "Quantity In Stock", "QuantityInStock");
        var reorderCol = Col("Reorder Level", "ReorderLevel");
        var batchCol = Col("Batch", "Batch Number", "BatchNumber");
        var expiryCol = Col("Expiry", "Expiry Date", "ExpiryDate");

        if (nameCol == -1)
        {
            result.Errors.Add("No 'Name' column found in the file — cannot import.");
            return result;
        }

        var existingKeys = _allRows
            .Select(r => (Name: r.Name.Trim().ToLowerInvariant(), Batch: r.BatchNumber.Trim().ToLowerInvariant()))
            .ToHashSet();

        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

        for (int r = 2; r <= lastRow; r++)
        {
            var name = ws.Cell(r, nameCol).GetString().Trim();
            if (string.IsNullOrWhiteSpace(name)) continue;

            var batch = batchCol > 0 ? ws.Cell(r, batchCol).GetString().Trim() : "";
            var key = (name.ToLowerInvariant(), batch.ToLowerInvariant());

            if (existingKeys.Contains(key))
            {
                result.Skipped++;
                continue;
            }

            try
            {
                var medicine = new Medicine
                {
                    Name = name,
                    GenericName = genericCol > 0 ? ws.Cell(r, genericCol).GetString().Trim() : null,
                    Category = categoryCol > 0 ? ws.Cell(r, categoryCol).GetString().Trim() : null,
                    Supplier = supplierCol > 0 ? ws.Cell(r, supplierCol).GetString().Trim() : null,
                    Manufacturer = manufacturerCol > 0 ? ws.Cell(r, manufacturerCol).GetString().Trim() : null,
                    UnitPrice = priceCol > 0 ? ws.Cell(r, priceCol).GetValue<decimal>() : 0,
                    CostPrice = costCol > 0 ? ws.Cell(r, costCol).GetValue<decimal>() : 0,
                    QuantityInStock = stockCol > 0 ? ws.Cell(r, stockCol).GetValue<int>() : 0,
                    ReorderLevel = reorderCol > 0 ? ws.Cell(r, reorderCol).GetValue<int>() : 10,
                    BatchNumber = string.IsNullOrWhiteSpace(batch) ? null : batch,
                    IsActive = true
                };

                if (expiryCol > 0)
                {
                    var expiryText = ws.Cell(r, expiryCol).GetString().Trim();
                    if (DateTime.TryParse(expiryText, out var expiry))
                        medicine.ExpiryDate = expiry;
                }

                await AddAsync(medicine);
                existingKeys.Add(key);
                result.Added++;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Row {r} ({name}): {ex.Message}");
            }
        }

        return result;
    }

    public async Task DeleteAsync(Medicine medicine)
    {
        await _repository.DeleteAsync(medicine.Id);
        Medicines.Remove(medicine);
        var row = _allRows.FirstOrDefault(r => r.Id == medicine.Id);
        if (row != null) { _allRows.Remove(row); Rows.Remove(row); }
    }

    public async Task<int> AddAsync(Medicine medicine)
    {
        var id = await _repository.CreateAsync(medicine);
        medicine.Id = id;
        Medicines.Add(medicine);
        await LoadAsync();
        return id;
    }

    public async Task UpdateAsync(Medicine medicine)
    {
        await _repository.UpdateAsync(medicine);
        await LoadAsync();
    }
}
