using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Application.Interfaces.Repositories;

public interface ISaleRepository
{
    Task<int> CreateSaleAsync(Sale sale);
    Task<List<Sale>> GetAllAsync();
    Task<Sale?> GetByInvoiceAsync(string invoiceNumber);
    Task<List<Sale>> GetByDateRangeAsync(DateTime from, DateTime to);
    Task<List<Sale>> GetCreditSalesAsync();
    Task RecordPaymentAsync(int saleId, decimal amount, string note = "");
    Task<List<PharmacyMS.Domain.Entities.SalePayment>> GetPaymentsAsync(int saleId);
    Task<Dictionary<int, DateTime>> GetLastPaymentDatesByCustomerAsync();
}
