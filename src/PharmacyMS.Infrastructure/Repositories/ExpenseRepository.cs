using Dapper;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Domain.Entities;
using PharmacyMS.Infrastructure.Data;
using PharmacyMS.Infrastructure.Services;

namespace PharmacyMS.Infrastructure.Repositories;

public class ExpenseRepository : IExpenseRepository
{
    private readonly AppDbContext _context;
    private readonly CodeGeneratorService _codeGen;
    public ExpenseRepository(AppDbContext context, CodeGeneratorService codeGen)
    {
        _context = context;
        _codeGen = codeGen;
    }

    public async Task<IEnumerable<Expense>> GetAllAsync()
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<Expense>("SELECT * FROM Expenses ORDER BY Date DESC");
    }

    public async Task<IEnumerable<Expense>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        using var conn = _context.CreateConnection();
        // Half-open interval [From, To+1day) avoids text-comparison boundary bugs
        // when Date is stored with a time component (e.g. "2026-08-19 00:00:00").
        return await conn.QueryAsync<Expense>(
            "SELECT * FROM Expenses WHERE Date >= @From AND Date < @ToExclusive ORDER BY Date DESC",
            new { From = from.Date.ToString("yyyy-MM-dd"), ToExclusive = to.Date.AddDays(1).ToString("yyyy-MM-dd") });
    }

    public async Task<int> CreateAsync(Expense expense)
    {
        expense.Code = await _codeGen.GetNextCodeAsync("EXP");
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<int>($@"
            INSERT INTO Expenses (Code, Date, Category, Description, Amount, PaymentMethod, CreatedBy, CreatedAt)
            VALUES (@Code, @Date, @Category, @Description, @Amount, @PaymentMethod, @CreatedBy, @CreatedAt)
            {_context.InsertIdSuffix()};", expense);
    }

    public async Task UpdateAsync(Expense expense)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE Expenses SET Date=@Date, Category=@Category, Description=@Description, Amount=@Amount, PaymentMethod=@PaymentMethod WHERE Id=@Id",
            expense);
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM Expenses WHERE Id=@Id", new { Id = id });
    }

    public async Task<decimal> GetTotalByDateRangeAsync(DateTime from, DateTime to)
    {
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<decimal>(
            "SELECT COALESCE(SUM(Amount),0) FROM Expenses WHERE Date >= @From AND Date < @ToExclusive",
            new { From = from.Date.ToString("yyyy-MM-dd"), ToExclusive = to.Date.AddDays(1).ToString("yyyy-MM-dd") });
    }
}
