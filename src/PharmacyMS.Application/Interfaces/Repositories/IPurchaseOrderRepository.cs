using PharmacyMS.Domain.Entities;
using PharmacyMS.Domain.Enums;

namespace PharmacyMS.Application.Interfaces.Repositories;

public interface IPurchaseOrderRepository
{
    Task<int> CreateAsync(PurchaseOrder order);
    Task<IEnumerable<PurchaseOrder>> GetAllAsync();
    Task<PurchaseOrder?> GetByIdAsync(int id);
    Task<IEnumerable<PurchaseOrder>> GetPendingReceivingAsync();
    Task UpdateStatusAsync(int id, PurchaseOrderStatus status);
}
