using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Application.Interfaces.Services;

public interface IPurchaseOrderPdfService
{
    Task<string> GeneratePdfAsync(PurchaseOrder order, Supplier supplier);
}
