using PharmacyMS.Domain.Entities;
using PharmacyMS.Domain.Enums;

namespace PharmacyMS.Application.Interfaces.Repositories;

public interface IPendingExpenseRepository
{
    Task<int> CreateAsync(PendingExpense expense);
    Task<List<PendingExpense>> GetPendingAsync();
    Task<List<PendingExpense>> GetBySubmitterAsync(int userId);
    Task<PendingExpense?> GetByIdAsync(int id);
    Task UpdateStatusAsync(int id, ApprovalStatus status, string? rejectionReason = null);
}
