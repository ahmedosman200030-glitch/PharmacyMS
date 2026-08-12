using Microsoft.AspNetCore.Mvc;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryRepository _repo;
    public CategoriesController(ICategoryRepository repo) => _repo = repo;

    [HttpGet] public async Task<IActionResult> GetAll() => Ok(await _repo.GetAllAsync());
    [HttpPost] public async Task<IActionResult> Create([FromBody] Category c) => Ok(await _repo.CreateAsync(c));
    [HttpPut("{id}")] public async Task<IActionResult> Update(int id, [FromBody] Category c) { c.Id = id; await _repo.UpdateAsync(c); return Ok(); }
    [HttpDelete("{id}")] public async Task<IActionResult> Delete(int id) { await _repo.DeleteAsync(id); return Ok(); }
}
