using System.Collections.ObjectModel;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Application.Services;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Desktop.ViewModels;

public class StockAdjustmentViewModel
{
    private readonly IMedicineRepository _medicineRepo;
    private readonly IStockAdjustmentRepository _adjustmentRepo;

    public ObservableCollection<Medicine> AvailableMedicines { get; } = new();
    public ObservableCollection<StockAdjustment> RecentAdjustments { get; } = new();

    public StockAdjustmentViewModel(IMedicineRepository medicineRepo, IStockAdjustmentRepository adjustmentRepo)
    {
        _medicineRepo = medicineRepo;
        _adjustmentRepo = adjustmentRepo;
    }

    public async Task LoadAsync()
    {
        AvailableMedicines.Clear();
        var meds = await _medicineRepo.GetAllAsync();
        foreach (var m in meds)
            AvailableMedicines.Add(m);

        RecentAdjustments.Clear();
        var recent = await _adjustmentRepo.GetRecentAsync(50);
        foreach (var r in recent)
            RecentAdjustments.Add(r);
    }

    /// <summary>
    /// Throws InvalidOperationException if the adjustment would take stock negative.
    /// </summary>
    public async Task SubmitAsync(Medicine medicine, int quantityChange, string reason)
    {
        var adjustment = new StockAdjustment
        {
            MedicineId = medicine.Id,
            MedicineName = medicine.Name,
            QuantityChange = quantityChange,
            Reason = reason,
            AdjustedByUserId = SessionManager.CurrentUser?.Id ?? 0,
            AdjustedByName = SessionManager.CurrentUser?.FullName ?? "Unknown"
        };

        await _adjustmentRepo.CreateAsync(adjustment);
        await LoadAsync();
    }
}
