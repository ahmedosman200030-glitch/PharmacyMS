using Microsoft.AspNetCore.Mvc;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _repo;
    public UsersController(IUserRepository repo) => _repo = repo;

    [HttpGet] public async Task<IActionResult> GetAll() => Ok(await _repo.GetAllAsync());
    [HttpGet("{id}")] public async Task<IActionResult> GetById(int id) => Ok(await _repo.GetByIdAsync(id));
    [HttpGet("username/{username}")] public async Task<IActionResult> GetByUsername(string username) => Ok(await _repo.GetByUsernameAsync(username));
    [HttpPost] public async Task<IActionResult> Create([FromBody] User u) => Ok(await _repo.CreateAsync(u));
    [HttpPut("{id}")] public async Task<IActionResult> Update(int id, [FromBody] User u) { u.Id = id; await _repo.UpdateAsync(u); return Ok(); }
    [HttpDelete("{id}")] public async Task<IActionResult> Delete(int id) { await _repo.DeleteAsync(id); return Ok(); }
    [HttpPost("{id}/activate")] public async Task<IActionResult> Activate(int id) { await _repo.ActivateAsync(id); return Ok(); }
}
