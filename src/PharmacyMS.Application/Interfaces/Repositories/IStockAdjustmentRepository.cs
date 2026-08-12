using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Application.Interfaces.Repositories;

public interface IStockAdjustmentRepository
{
    Task<int> CreateAsync(StockAdjustment adjustment);
    Task<IEnumerable<StockAdjustment>> GetRecentAsync(int limit = 50);
}
