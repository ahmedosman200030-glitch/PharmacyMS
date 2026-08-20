using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Application.Interfaces.Repositories;

public interface IGoodsReceiptRepository
{
    Task<int> ReceiveAsync(GoodsReceipt receipt);
    Task<IEnumerable<GoodsReceipt>> GetAllAsync();
    Task<GoodsReceipt?> GetByIdAsync(int id);
    Task<GoodsReceipt?> GetByIdWithOrderNumberAsync(int id);
    Task ApproveAsync(int receiptId);
    Task RejectAsync(int receiptId, string reason);
}
