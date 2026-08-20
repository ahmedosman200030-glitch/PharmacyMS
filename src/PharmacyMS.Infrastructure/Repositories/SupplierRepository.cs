using Dapper;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Domain.Entities;
using PharmacyMS.Domain.Enums;
using PharmacyMS.Infrastructure.Data;

namespace PharmacyMS.Infrastructure.Repositories;

public class SupplierRepository : ISupplierRepository
{
    private readonly AppDbContext _context;
    public SupplierRepository(AppDbContext context) => _context = context;

    public async Task<IEnumerable<Supplier>> GetAllAsync()
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<Supplier>("SELECT * FROM Suppliers WHERE IsActive=1 ORDER BY Name");
    }

    public async Task<int> CreateAsync(Supplier supplier)
    {
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<int>($@"
            INSERT INTO Suppliers (Name, ContactPerson, Phone, Email, Address, IsActive, CreatedAt, ApprovalStatus, SubmittedByUserId, SubmittedByName)
            VALUES (@Name, @ContactPerson, @Phone, @Email, @Address, 1, {_context.NowExpr()}, @ApprovalStatus, @SubmittedByUserId, @SubmittedByName)
            {_context.InsertIdSuffix()};", supplier);
    }

    public async Task UpdateAsync(Supplier supplier)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(@"
            UPDATE Suppliers SET Name=@Name, ContactPerson=@ContactPerson,
            Phone=@Phone, Email=@Email, Address=@Address WHERE Id=@Id", supplier);
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync("UPDATE Suppliers SET IsActive=0 WHERE Id=@Id", new { Id = id });
    }

    public async Task UpdateApprovalStatusAsync(int id, ApprovalStatus status)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync("UPDATE Suppliers SET ApprovalStatus=@Status WHERE Id=@Id",
            new { Id = id, Status = status });
    }

    public async Task<IEnumerable<Supplier>> GetBySubmitterAsync(int userId)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<Supplier>(
            "SELECT * FROM Suppliers WHERE SubmittedByUserId=@UserId ORDER BY CreatedAt DESC",
            new { UserId = userId });
    }
}
