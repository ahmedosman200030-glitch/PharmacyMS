using Dapper;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Domain.Entities;
using PharmacyMS.Infrastructure.Data;

namespace PharmacyMS.Infrastructure.Repositories;

public class MedicineRepository : IMedicineRepository
{
    private readonly AppDbContext _context;
    public MedicineRepository(AppDbContext context) => _context = context;

    public async Task<IEnumerable<Medicine>> GetAllAsync()
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<Medicine>("SELECT * FROM Medicines WHERE IsActive=1 ORDER BY Name");
    }

    public async Task<Medicine?> GetByIdAsync(int id)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Medicine>("SELECT * FROM Medicines WHERE Id=@Id", new { Id = id });
    }

    public async Task<IEnumerable<Medicine>> SearchAsync(string term)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<Medicine>(
            "SELECT * FROM Medicines WHERE IsActive=1 AND (Name LIKE @T OR GenericName LIKE @T) ORDER BY Name",
            new { T = $"%{term}%" });
    }

    public async Task<IEnumerable<Medicine>> GetLowStockAsync()
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<Medicine>(
            "SELECT * FROM Medicines WHERE IsActive=1 AND QuantityInStock <= ReorderLevel ORDER BY Name");
    }

    public async Task<int> CreateAsync(Medicine medicine)
    {
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<int>($@"
            INSERT INTO Medicines (Name, GenericName, Category, Manufacturer, UnitPrice, CostPrice,
            QuantityInStock, ReorderLevel, ExpiryDate, BatchNumber, Barcode, Supplier, Unit, IsActive, CreatedAt)
            VALUES (@Name, @GenericName, @Category, @Manufacturer, @UnitPrice, @CostPrice,
            @QuantityInStock, @ReorderLevel, @ExpiryDate, @BatchNumber, @Barcode, @Supplier, @Unit, 1, {_context.NowExpr()})
            {_context.InsertIdSuffix()};", medicine);
    }

    public async Task UpdateAsync(Medicine medicine)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync($@"
            UPDATE Medicines SET Name=@Name, GenericName=@GenericName, Category=@Category,
            Manufacturer=@Manufacturer, UnitPrice=@UnitPrice, CostPrice=@CostPrice,
            QuantityInStock=@QuantityInStock, ReorderLevel=@ReorderLevel, ExpiryDate=@ExpiryDate,
            BatchNumber=@BatchNumber, Barcode=@Barcode, Supplier=@Supplier, Unit=@Unit, UpdatedAt={_context.NowExpr()}
            WHERE Id=@Id", medicine);
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync("UPDATE Medicines SET IsActive=0 WHERE Id=@Id", new { Id = id });
    }
}
