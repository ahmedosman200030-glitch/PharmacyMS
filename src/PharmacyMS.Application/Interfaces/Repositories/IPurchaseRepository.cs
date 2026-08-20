using PharmacyMS.Domain.Entities;
using PharmacyMS.Domain.Enums;

namespace PharmacyMS.Application.Interfaces.Repositories;

public interface IPurchaseRepository
{
    Task<int> CreatePurchaseAsync(Purchase purchase);
    Task<int> CreateBillFromReceiptAsync(int goodsReceiptId, string? invoiceNumber);
    Task<IEnumerable<Purchase>> GetAllAsync();
    Task<Purchase?> GetByIdAsync(int id);
    Task<IEnumerable<SupplierSpend>> GetSpendBySupplierAsync();
    Task<IEnumerable<MedicineSpend>> GetSpendByMedicineAsync();
    Task<decimal> GetTotalSpendAsync();
    Task RecordPaymentAsync(int purchaseId, decimal amount, string note = "");
    Task<List<PurchasePayment>> GetPaymentsAsync(int purchaseId);
    Task UpdateApprovalStatusAsync(int purchaseId, ApprovalStatus status);
    Task<Dictionary<int, DateTime>> GetLastPaymentDatesBySupplierAsync();
}
