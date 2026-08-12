using Dapper;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Domain.Entities;
using PharmacyMS.Infrastructure.Data;

namespace PharmacyMS.Infrastructure.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _context;

    public CustomerRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Customer?> GetByIdAsync(int id)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Customer>(
            "SELECT * FROM Customers WHERE Id = @Id", new { Id = id });
    }

    public async Task<IEnumerable<Customer>> GetAllAsync()
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<Customer>(
            "SELECT * FROM Customers WHERE IsActive = 1 ORDER BY Name");
    }

    public async Task<IEnumerable<Customer>> SearchAsync(string term)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<Customer>(
            @"SELECT * FROM Customers
              WHERE IsActive = 1
                AND (Name LIKE @Term OR Phone LIKE @Term OR Email LIKE @Term)
              ORDER BY Name",
            new { Term = $"%{term}%" });
    }

    public async Task<int> CreateAsync(Customer customer)
    {
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO Customers (Name, Phone, Email, Address, IsActive, CreatedAt)
            VALUES (@Name, @Phone, @Email, @Address, @IsActive, datetime('now'));
            SELECT last_insert_rowid();",
            customer);
    }

    public async Task UpdateAsync(Customer customer)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(@"
            UPDATE Customers SET
                Name = @Name,
                Phone = @Phone,
                Email = @Email,
                Address = @Address,
                IsActive = @IsActive,
                UpdatedAt = datetime('now')
            WHERE Id = @Id",
            customer);
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE Customers SET IsActive = 0 WHERE Id = @Id", new { Id = id });
    }

    public async Task<Customer> GetOrCreateByNameAsync(string name)
    {
        using var conn = _context.CreateConnection();
        var trimmed = name.Trim();

        var existing = await conn.QueryFirstOrDefaultAsync<Customer>(
            "SELECT * FROM Customers WHERE IsActive = 1 AND Name = @Name COLLATE NOCASE",
            new { Name = trimmed });

        if (existing != null) return existing;

        var newCustomer = new Customer { Name = trimmed, IsActive = true };
        var id = await CreateAsync(newCustomer);
        newCustomer.Id = id;
        return newCustomer;
    }

    public async Task<decimal> GetOutstandingBalanceAsync(int customerId)
    {
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<decimal>(
            @"SELECT COALESCE(SUM(TotalAmount - AmountPaid), 0)
              FROM Sales
              WHERE CustomerId = @CustomerId AND TotalAmount > AmountPaid",
            new { CustomerId = customerId });
    }
}
