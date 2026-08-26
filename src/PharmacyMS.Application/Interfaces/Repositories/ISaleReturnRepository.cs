using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Application.Interfaces.Repositories;

public interface ISaleReturnRepository
{
    Task<int> CreateAsync(SaleReturn saleReturn);
    Task<List<SaleReturn>> GetRecentAsync(int limit = 50);
    Task<List<SaleReturn>> GetByDateRangeAsync(DateTime from, DateTime to);
    Task<List<SaleReturn>> GetByOriginalSaleIdAsync(int saleId);
}
