using System.Collections.ObjectModel;
using PharmacyMS.Application.Enums;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Application.Interfaces.Services;
using PharmacyMS.Application.Services;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Desktop.ViewModels;

public class CartLine : System.ComponentModel.INotifyPropertyChanged
{
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    private void OnChanged(string name) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));

    public int MedicineId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Batch { get; set; } = string.Empty;
    public string Unit { get; set; } = "Box";
    public decimal UnitPrice { get; set; }
    public int MaxQuantity { get; set; }

    private int _quantity;
    public int Quantity
    {
        get => _quantity;
        set { _quantity = value; OnChanged(nameof(Quantity)); OnChanged(nameof(LineSubtotal)); OnChanged(nameof(LineDiscountAmount)); OnChanged(nameof(LineTotal)); }
    }

    private decimal _discountPercent;
    public decimal DiscountPercent
    {
        get => _discountPercent;
        set { _discountPercent = value; OnChanged(nameof(DiscountPercent)); OnChanged(nameof(LineDiscountAmount)); OnChanged(nameof(LineTotal)); }
    }

    public decimal LineSubtotal => UnitPrice * Quantity;
    public decimal LineDiscountAmount => LineSubtotal * (DiscountPercent / 100m);
    public decimal LineTotal => LineSubtotal - LineDiscountAmount;
}

public class HeldSale
{
    public string Label { get; set; } = "";
    public List<CartLine> Lines { get; set; } = new();
    public Customer? Customer { get; set; }
}

public class PosViewModel
{
    private readonly IMedicineRepository _medicineRepo;
    private readonly ISaleRepository _saleRepo;
    private readonly IAppSettingsService _settingsService;
    private readonly ICustomerRepository _customerRepo;
    private readonly ISoundService _soundService;

    private List<Medicine> _allMedicines = new();

    public ObservableCollection<Medicine> AvailableMedicines { get; } = new();
    public ObservableCollection<CartLine> Cart { get; } = new();
    public ObservableCollection<string> Categories { get; } = new();
    public ObservableCollection<Customer> Customers { get; } = new();
    public List<HeldSale> HeldSales { get; } = new();

    public decimal TaxRate { get; private set; } = 0.15m;
    public decimal AmountReceived { get; set; } = 0m;
    public Customer? SelectedCustomer { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public bool IsPrescription { get; set; }
    public string Notes { get; set; } = "";
    public bool IsCreditSale { get; set; }
    public string CreditCustomerName { get; set; } = "";

    public decimal Subtotal => Cart.Sum(c => c.LineSubtotal);
    public decimal TotalDiscount => Cart.Sum(c => c.LineDiscountAmount);
    public decimal DiscountedSubtotal => Subtotal - TotalDiscount;
    public decimal TaxAmount => DiscountedSubtotal * TaxRate;
    public decimal Total => DiscountedSubtotal + TaxAmount;
    public decimal ChangeDue => Math.Max(0, AmountReceived - Total);

    public PosViewModel(
        IMedicineRepository medicineRepo,
        ISaleRepository saleRepo,
        IAppSettingsService settingsService,
        ICustomerRepository customerRepo,
        ISoundService soundService)
    {
        _medicineRepo = medicineRepo;
        _saleRepo = saleRepo;
        _settingsService = settingsService;
        _customerRepo = customerRepo;
        _soundService = soundService;
    }

    public async Task LoadAsync()
    {
        TaxRate = await _settingsService.GetTaxRateAsync();

        _allMedicines = (await _medicineRepo.GetAllAsync()).ToList();
        ApplyFilter(null, null);

        Categories.Clear();
        Categories.Add("All");
        foreach (var cat in _allMedicines
            .Select(m => string.IsNullOrWhiteSpace(m.Category) ? "Uncategorized" : m.Category)
            .Distinct()
            .OrderBy(c => c))
            Categories.Add(cat);

        Customers.Clear();
        Customers.Add(new Customer { Id = 0, Name = "Walk-in Customer" });
        foreach (var c in await _customerRepo.GetAllAsync())
            Customers.Add(c);
    }

    public void ApplyFilter(string? searchText, string? category)
    {
        AvailableMedicines.Clear();
        var query = _allMedicines.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var s = searchText.Trim().ToLowerInvariant();
            query = query.Where(m =>
                m.Name.ToLowerInvariant().Contains(s) ||
                (m.GenericName ?? "").ToLowerInvariant().Contains(s) ||
                (m.BatchNumber ?? "").ToLowerInvariant().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(category) && category != "All")
        {
            query = query.Where(m =>
                (string.IsNullOrWhiteSpace(m.Category) ? "Uncategorized" : m.Category) == category);
        }

        foreach (var m in query)
            AvailableMedicines.Add(m);
    }

    public bool AddToCart(Medicine medicine, int qty)
    {
        if (qty <= 0) return false;

        var existing = Cart.FirstOrDefault(c => c.MedicineId == medicine.Id);
        var alreadyInCart = existing?.Quantity ?? 0;
        if (alreadyInCart + qty > medicine.QuantityInStock) return false;

        if (existing != null)
        {
            existing.Quantity += qty;
        }
        else
        {
            Cart.Add(new CartLine
            {
                MedicineId = medicine.Id,
                Name = medicine.Name,
                Batch = medicine.BatchNumber ?? "",
                Unit = medicine.Unit,
                UnitPrice = medicine.UnitPrice,
                Quantity = qty,
                MaxQuantity = medicine.QuantityInStock,
                DiscountPercent = 0
            });
        }

        _soundService.Play(SoundEvent.ItemAdded);
        return true;
    }

    public void IncreaseQty(CartLine line)
    {
        if (line.Quantity < line.MaxQuantity) line.Quantity++;
    }

    public void DecreaseQty(CartLine line)
    {
        if (line.Quantity > 1) line.Quantity--;
        else Cart.Remove(line);
    }

    public void IncreaseDiscount(CartLine line)
    {
        if (line.DiscountPercent <= 95) line.DiscountPercent += 5;
    }

    public void DecreaseDiscount(CartLine line)
    {
        if (line.DiscountPercent >= 5) line.DiscountPercent -= 5;
    }

    public void RemoveFromCart(CartLine line) => Cart.Remove(line);

    public void ClearCart()
    {
        Cart.Clear();
        AmountReceived = 0m;
        SelectedCustomer = null;
        PaymentMethod = "Cash";
        IsPrescription = false;
        Notes = "";
        IsCreditSale = false;
        CreditCustomerName = "";
    }

    public void HoldCurrentSale(string label)
    {
        if (Cart.Count == 0) return;
        HeldSales.Add(new HeldSale
        {
            Label = label,
            Lines = Cart.ToList(),
            Customer = SelectedCustomer
        });
        ClearCart();
    }

    public void RecallHeldSale(HeldSale held)
    {
        ClearCart();
        foreach (var line in held.Lines)
            Cart.Add(line);
        SelectedCustomer = held.Customer;
        HeldSales.Remove(held);
    }

    public async Task<decimal> GetOutstandingBalanceAsync(int customerId)
        => customerId > 0 ? await _customerRepo.GetOutstandingBalanceAsync(customerId) : 0m;

    public event Action<Sale>? SaleCompleted;

    public async Task<Sale> CheckoutAsync()
    {
        if (IsCreditSale && !string.IsNullOrWhiteSpace(CreditCustomerName)
            && (SelectedCustomer == null || SelectedCustomer.Id == 0))
        {
            SelectedCustomer = await _customerRepo.GetOrCreateByNameAsync(CreditCustomerName);
        }

        var sale = new Sale
        {
            InvoiceNumber = "INV-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
            CashierId = SessionManager.CurrentUser?.Id ?? 0,
            Subtotal = DiscountedSubtotal,
            TaxRate = TaxRate,
            TaxAmount = TaxAmount,
            TotalAmount = Total,
            CustomerId = SelectedCustomer?.Id,
            CustomerName = SelectedCustomer?.Name ?? "Walk-in Customer",
            PaymentMethod = PaymentMethod,
            TotalDiscount = TotalDiscount,
            AmountPaid = Math.Min(AmountReceived, Total),
            ChangeDue = ChangeDue,
            CreatedAt = DateTime.Now,
            Items = Cart.Select(c => new SaleItem
            {
                MedicineId = c.MedicineId,
                MedicineName = c.Name,
                Unit = c.Unit,
                UnitPrice = c.UnitPrice,
                Quantity = c.Quantity
            }).ToList()
        };

        var id = await _saleRepo.CreateSaleAsync(sale);
        sale.Id = id;
        _soundService.Play(SoundEvent.TransactionSuccess);
        SaleCompleted?.Invoke(sale);
        ClearCart();
        await LoadAsync();
        return sale;
    }
}
