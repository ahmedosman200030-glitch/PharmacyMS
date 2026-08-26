using Dapper;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Domain.Entities;
using PharmacyMS.Infrastructure.Data;
using PharmacyMS.Infrastructure.Services;

namespace PharmacyMS.Infrastructure.Repositories;

public class OtherIncomeRepository : IOtherIncomeRepository
{
    private readonly AppDbContext _context;
    private readonly CodeGeneratorService _codeGen;
    public OtherIncomeRepository(AppDbContext context, CodeGeneratorService codeGen)
    {
        _context = context;
        _codeGen = codeGen;
    }

    public async Task<IEnumerable<OtherIncome>> GetAllAsync()
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<OtherIncome>("SELECT * FROM OtherIncomes ORDER BY Date DESC");
    }

    public async Task<IEnumerable<OtherIncome>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<OtherIncome>(
            "SELECT * FROM OtherIncomes WHERE Date >= @From AND Date < @ToExclusive ORDER BY Date DESC",
            new { From = from.Date.ToString("yyyy-MM-dd"), ToExclusive = to.Date.AddDays(1).ToString("yyyy-MM-dd") });
    }

    public async Task<int> CreateAsync(OtherIncome income)
    {
        income.Code = await _codeGen.GetNextCodeAsync("INC");
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<int>($@"
            INSERT INTO OtherIncomes (Code, Date, Category, Description, Amount, PaymentMethod, CreatedBy, CreatedAt)
            VALUES (@Code, @Date, @Category, @Description, @Amount, @PaymentMethod, @CreatedBy, @CreatedAt)
            {_context.InsertIdSuffix()};", income);
    }

    public async Task UpdateAsync(OtherIncome income)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE OtherIncomes SET Date=@Date, Category=@Category, Description=@Description, Amount=@Amount, PaymentMethod=@PaymentMethod WHERE Id=@Id",
            income);
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM OtherIncomes WHERE Id=@Id", new { Id = id });
    }

    public async Task<decimal> GetTotalByDateRangeAsync(DateTime from, DateTime to)
    {
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<decimal>(
            "SELECT COALESCE(SUM(Amount),0) FROM OtherIncomes WHERE Date >= @From AND Date < @ToExclusive",
            new { From = from.Date.ToString("yyyy-MM-dd"), ToExclusive = to.Date.AddDays(1).ToString("yyyy-MM-dd") });
    }
}
