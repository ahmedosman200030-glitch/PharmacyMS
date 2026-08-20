using PharmacyMS.Domain.Entities;
using PharmacyMS.Domain.Enums;

namespace PharmacyMS.Application.Interfaces.Repositories;

public interface IPendingSalePaymentRepository
{
    Task<int> CreateAsync(PendingSalePayment payment);
    Task<List<PendingSalePayment>> GetPendingAsync();
    Task<List<PendingSalePayment>> GetBySubmitterAsync(int userId);
    Task<PendingSalePayment?> GetByIdAsync(int id);
    Task UpdateStatusAsync(int id, ApprovalStatus status, string? rejectionReason = null);
}
