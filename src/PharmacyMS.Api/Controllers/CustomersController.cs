using Microsoft.AspNetCore.Mvc;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerRepository _repo;
    public CustomersController(ICustomerRepository repo) => _repo = repo;

    [HttpGet] public async Task<IActionResult> GetAll() => Ok(await _repo.GetAllAsync());
    [HttpGet("{id}")] public async Task<IActionResult> GetById(int id) => Ok(await _repo.GetByIdAsync(id));
    [HttpGet("search")] public async Task<IActionResult> Search([FromQuery] string term) => Ok(await _repo.SearchAsync(term));
    [HttpGet("{id}/balance")] public async Task<IActionResult> Balance(int id) => Ok(await _repo.GetOutstandingBalanceAsync(id));
    [HttpPost] public async Task<IActionResult> Create([FromBody] Customer c) => Ok(await _repo.CreateAsync(c));
    [HttpPost("get-or-create")] public async Task<IActionResult> GetOrCreate([FromBody] NameRequest req) => Ok(await _repo.GetOrCreateByNameAsync(req.Name));
    [HttpPut("{id}")] public async Task<IActionResult> Update(int id, [FromBody] Customer c) { c.Id = id; await _repo.UpdateAsync(c); return Ok(); }
    [HttpDelete("{id}")] public async Task<IActionResult> Delete(int id) { await _repo.DeleteAsync(id); return Ok(); }
}

public record NameRequest(string Name);
