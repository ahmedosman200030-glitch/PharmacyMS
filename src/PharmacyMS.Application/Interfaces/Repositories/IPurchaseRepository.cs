using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Application.Interfaces.Repositories;

public interface IPurchaseRepository
{
    Task<int> CreatePurchaseAsync(Purchase purchase);
    Task<IEnumerable<Purchase>> GetAllAsync();
    Task<Purchase?> GetByIdAsync(int id);
    Task<IEnumerable<SupplierSpend>> GetSpendBySupplierAsync();
    Task<IEnumerable<MedicineSpend>> GetSpendByMedicineAsync();
    Task<decimal> GetTotalSpendAsync();
    Task RecordPaymentAsync(int purchaseId, decimal amount);
}
