using Dapper;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Domain.Entities;
using PharmacyMS.Domain.Enums;
using PharmacyMS.Infrastructure.Data;

namespace PharmacyMS.Infrastructure.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _context;
    public CustomerRepository(AppDbContext context) => _context = context;

    public async Task<IEnumerable<Customer>> GetAllAsync()
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<Customer>("SELECT * FROM Customers WHERE IsActive=1 ORDER BY Name");
    }

    public async Task<Customer?> GetByIdAsync(int id)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Customer>("SELECT * FROM Customers WHERE Id=@Id", new { Id = id });
    }

    public async Task<IEnumerable<Customer>> SearchAsync(string term)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<Customer>(
            "SELECT * FROM Customers WHERE IsActive=1 AND Name LIKE @T ORDER BY Name", new { T = $"%{term}%" });
    }

    public async Task<int> CreateAsync(Customer customer)
    {
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<int>($@"
            INSERT INTO Customers (Name, Phone, Email, Address, IsActive, CreatedAt, ApprovalStatus, SubmittedByUserId, SubmittedByName)
            VALUES (@Name, @Phone, @Email, @Address, 1, {_context.NowExpr()}, @ApprovalStatus, @SubmittedByUserId, @SubmittedByName)
            {_context.InsertIdSuffix()};", customer);
    }

    public async Task UpdateAsync(Customer customer)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync($@"
            UPDATE Customers SET Name=@Name, Phone=@Phone, Email=@Email,
            Address=@Address, UpdatedAt={_context.NowExpr()} WHERE Id=@Id", customer);
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync("UPDATE Customers SET IsActive=0 WHERE Id=@Id", new { Id = id });
    }

    public async Task UpdateApprovalStatusAsync(int id, ApprovalStatus status)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync("UPDATE Customers SET ApprovalStatus=@Status WHERE Id=@Id",
            new { Id = id, Status = status });
    }

    public async Task<Customer> GetOrCreateByNameAsync(string name)
    {
        using var conn = _context.CreateConnection();
        var existing = await conn.QueryFirstOrDefaultAsync<Customer>(
            "SELECT * FROM Customers WHERE Name=@Name AND IsActive=1 LIMIT 1", new { Name = name });
        if (existing != null) return existing;
        var id = await conn.ExecuteScalarAsync<int>($@"
            INSERT INTO Customers (Name, IsActive, CreatedAt) VALUES (@Name, 1, {_context.NowExpr()})
            {_context.InsertIdSuffix()};", new { Name = name });
        return new Customer { Id = id, Name = name, IsActive = true };
    }

    public async Task<decimal> GetOutstandingBalanceAsync(int customerId)
    {
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<decimal>(@"
            SELECT COALESCE(SUM(TotalAmount - AmountPaid), 0) FROM Sales
            WHERE CustomerId=@Id AND AmountPaid < TotalAmount", new { Id = customerId });
    }

    public async Task<IEnumerable<Customer>> GetBySubmitterAsync(int userId)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<Customer>(
            "SELECT * FROM Customers WHERE SubmittedByUserId=@UserId ORDER BY CreatedAt DESC",
            new { UserId = userId });
    }
}
