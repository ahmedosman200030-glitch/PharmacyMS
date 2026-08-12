using System.Collections.ObjectModel;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Application.Services;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Desktop.ViewModels;

public class DailyClosingViewModel
{
    private readonly ISaleRepository _saleRepo;
    private readonly IDailyClosingRepository _closingRepo;

    public decimal CashSales { get; private set; }
    public decimal CardSales { get; private set; }
    public decimal MobileSales { get; private set; }
    public decimal InsuranceSales { get; private set; }
    public decimal ExpectedCash => CashSales;
    public bool AlreadyClosedToday { get; private set; }

    public ObservableCollection<DailyClosing> History { get; } = new();

    public DailyClosingViewModel(ISaleRepository saleRepo, IDailyClosingRepository closingRepo)
    {
        _saleRepo = saleRepo;
        _closingRepo = closingRepo;
    }

    public async Task LoadAsync()
    {
        var from = DateTime.Today;
        var to = DateTime.Today.AddDays(1).AddSeconds(-1);
        var todaySales = await _saleRepo.GetByDateRangeAsync(from, to);

        CashSales = todaySales.Where(s => s.PaymentMethod == "Cash").Sum(s => s.AmountPaid);
        CardSales = todaySales.Where(s => s.PaymentMethod == "Card").Sum(s => s.AmountPaid);
        MobileSales = todaySales.Where(s => s.PaymentMethod == "Mobile Money").Sum(s => s.AmountPaid);
        InsuranceSales = todaySales.Where(s => s.PaymentMethod == "Insurance").Sum(s => s.AmountPaid);

        AlreadyClosedToday = await _closingRepo.HasClosedTodayAsync();

        var history = await _closingRepo.GetHistoryAsync();
        History.Clear();
        foreach (var h in history) History.Add(h);
    }

    public async Task<DailyClosing> CloseRegisterAsync(decimal actualCash, string? notes)
    {
        var closing = new DailyClosing
        {
            ClosingDate = DateTime.Today,
            CashSales = CashSales,
            CardSales = CardSales,
            MobileSales = MobileSales,
            InsuranceSales = InsuranceSales,
            ExpectedCash = ExpectedCash,
            ActualCash = actualCash,
            Difference = actualCash - ExpectedCash,
            Notes = notes,
            ClosedByUserId = SessionManager.CurrentUser?.Id ?? 0,
            ClosedByUserName = SessionManager.CurrentUser?.FullName ?? "Unknown"
        };

        var id = await _closingRepo.CreateAsync(closing);
        closing.Id = id;
        await LoadAsync();
        return closing;
    }
}
