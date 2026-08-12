using Microsoft.AspNetCore.Mvc;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SuppliersController : ControllerBase
{
    private readonly ISupplierRepository _repo;
    public SuppliersController(ISupplierRepository repo) => _repo = repo;

    [HttpGet] public async Task<IActionResult> GetAll() => Ok(await _repo.GetAllAsync());
    [HttpPost] public async Task<IActionResult> Create([FromBody] Supplier s) => Ok(await _repo.CreateAsync(s));
    [HttpPut("{id}")] public async Task<IActionResult> Update(int id, [FromBody] Supplier s) { s.Id = id; await _repo.UpdateAsync(s); return Ok(); }
    [HttpDelete("{id}")] public async Task<IActionResult> Delete(int id) { await _repo.DeleteAsync(id); return Ok(); }
}
