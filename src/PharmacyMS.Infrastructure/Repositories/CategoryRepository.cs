using Dapper;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Domain.Entities;
using PharmacyMS.Infrastructure.Data;

namespace PharmacyMS.Infrastructure.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _context;
    public CategoryRepository(AppDbContext context) => _context = context;

    public async Task<IEnumerable<Category>> GetAllAsync()
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<Category>("SELECT * FROM Categories WHERE IsActive=1 ORDER BY Name");
    }

    public async Task<int> CreateAsync(Category category)
    {
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<int>($@"
            INSERT INTO Categories (Name, Description, IsActive, CreatedAt)
            VALUES (@Name, @Description, 1, {_context.NowExpr()})
            {_context.InsertIdSuffix()};", category);
    }

    public async Task UpdateAsync(Category category)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE Categories SET Name=@Name, Description=@Description WHERE Id=@Id", category);
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync("UPDATE Categories SET IsActive=0 WHERE Id=@Id", new { Id = id });
    }
}
