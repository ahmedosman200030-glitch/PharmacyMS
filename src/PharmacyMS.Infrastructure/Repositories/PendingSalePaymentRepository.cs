using Dapper;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Domain.Entities;
using PharmacyMS.Domain.Enums;
using PharmacyMS.Infrastructure.Data;

namespace PharmacyMS.Infrastructure.Repositories;

public class PendingSalePaymentRepository : IPendingSalePaymentRepository
{
    private readonly AppDbContext _context;
    public PendingSalePaymentRepository(AppDbContext context) => _context = context;

    public async Task<int> CreateAsync(PendingSalePayment payment)
    {
        using var conn = _context.CreateConnection();
        var sql = $@"
            INSERT INTO PendingSalePayments
                (SaleId, CustomerName, Amount, Note, SubmittedByUserId, SubmittedByName, SubmittedAt, ApprovalStatus)
            VALUES
                (@SaleId, @CustomerName, @Amount, @Note, @SubmittedByUserId, @SubmittedByName, @SubmittedAt, @ApprovalStatus);
            {_context.InsertIdSuffix()}";
        return await conn.ExecuteScalarAsync<int>(sql, new
        {
            payment.SaleId,
            payment.CustomerName,
            payment.Amount,
            payment.Note,
            payment.SubmittedByUserId,
            payment.SubmittedByName,
            SubmittedAt = payment.SubmittedAt,
            ApprovalStatus = (int)payment.ApprovalStatus
        });
    }

    public async Task<List<PendingSalePayment>> GetPendingAsync()
    {
        using var conn = _context.CreateConnection();
        var rows = await conn.QueryAsync<PendingSalePayment>(
            "SELECT * FROM PendingSalePayments WHERE ApprovalStatus = @Status ORDER BY SubmittedAt DESC",
            new { Status = (int)ApprovalStatus.Pending });
        return rows.ToList();
    }

    public async Task<List<PendingSalePayment>> GetBySubmitterAsync(int userId)
    {
        using var conn = _context.CreateConnection();
        var rows = await conn.QueryAsync<PendingSalePayment>(
            "SELECT * FROM PendingSalePayments WHERE SubmittedByUserId = @UserId ORDER BY SubmittedAt DESC",
            new { UserId = userId });
        return rows.ToList();
    }

    public async Task<PendingSalePayment?> GetByIdAsync(int id)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<PendingSalePayment>(
            "SELECT * FROM PendingSalePayments WHERE Id = @Id", new { Id = id });
    }

    public async Task UpdateStatusAsync(int id, ApprovalStatus status, string? rejectionReason = null)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE PendingSalePayments SET ApprovalStatus = @Status, RejectionReason = @Reason WHERE Id = @Id",
            new { Id = id, Status = (int)status, Reason = rejectionReason });
    }
}
