using Dapper;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Domain.Entities;
using PharmacyMS.Domain.Enums;
using PharmacyMS.Infrastructure.Data;

namespace PharmacyMS.Infrastructure.Repositories;

public class PendingExpenseRepository : IPendingExpenseRepository
{
    private readonly AppDbContext _context;
    public PendingExpenseRepository(AppDbContext context) => _context = context;

    public async Task<int> CreateAsync(PendingExpense expense)
    {
        using var conn = _context.CreateConnection();
        var sql = $@"
            INSERT INTO PendingExpenses
                (Date, Category, Description, Amount, SubmittedByUserId, SubmittedByName, SubmittedAt, ApprovalStatus)
            VALUES
                (@Date, @Category, @Description, @Amount, @SubmittedByUserId, @SubmittedByName, @SubmittedAt, @ApprovalStatus);
            {_context.InsertIdSuffix()}";
        return await conn.ExecuteScalarAsync<int>(sql, new
        {
            expense.Date,
            expense.Category,
            expense.Description,
            expense.Amount,
            expense.SubmittedByUserId,
            expense.SubmittedByName,
            SubmittedAt = expense.SubmittedAt,
            ApprovalStatus = (int)expense.ApprovalStatus
        });
    }

    public async Task<List<PendingExpense>> GetPendingAsync()
    {
        using var conn = _context.CreateConnection();
        var rows = await conn.QueryAsync<PendingExpense>(
            "SELECT * FROM PendingExpenses WHERE ApprovalStatus = @Status ORDER BY SubmittedAt DESC",
            new { Status = (int)ApprovalStatus.Pending });
        return rows.ToList();
    }

    public async Task<List<PendingExpense>> GetBySubmitterAsync(int userId)
    {
        using var conn = _context.CreateConnection();
        var rows = await conn.QueryAsync<PendingExpense>(
            "SELECT * FROM PendingExpenses WHERE SubmittedByUserId = @UserId ORDER BY SubmittedAt DESC",
            new { UserId = userId });
        return rows.ToList();
    }

    public async Task<PendingExpense?> GetByIdAsync(int id)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<PendingExpense>(
            "SELECT * FROM PendingExpenses WHERE Id = @Id", new { Id = id });
    }

    public async Task UpdateStatusAsync(int id, ApprovalStatus status, string? rejectionReason = null)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE PendingExpenses SET ApprovalStatus = @Status, RejectionReason = @Reason WHERE Id = @Id",
            new { Id = id, Status = (int)status, Reason = rejectionReason });
    }
}
