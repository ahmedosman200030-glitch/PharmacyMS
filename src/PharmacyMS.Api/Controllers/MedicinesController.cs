using Microsoft.AspNetCore.Mvc;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MedicinesController : ControllerBase
{
    private readonly IMedicineRepository _repo;
    public MedicinesController(IMedicineRepository repo) => _repo = repo;

    [HttpGet] public async Task<IActionResult> GetAll() => Ok(await _repo.GetAllAsync());
    [HttpGet("{id}")] public async Task<IActionResult> GetById(int id) => Ok(await _repo.GetByIdAsync(id));
    [HttpGet("search")] public async Task<IActionResult> Search([FromQuery] string term) => Ok(await _repo.SearchAsync(term));
    [HttpGet("low-stock")] public async Task<IActionResult> LowStock() => Ok(await _repo.GetLowStockAsync());
    [HttpPost] public async Task<IActionResult> Create([FromBody] Medicine m) => Ok(await _repo.CreateAsync(m));
    [HttpPut("{id}")] public async Task<IActionResult> Update(int id, [FromBody] Medicine m) { m.Id = id; await _repo.UpdateAsync(m); return Ok(); }
    [HttpDelete("{id}")] public async Task<IActionResult> Delete(int id) { await _repo.DeleteAsync(id); return Ok(); }
}
