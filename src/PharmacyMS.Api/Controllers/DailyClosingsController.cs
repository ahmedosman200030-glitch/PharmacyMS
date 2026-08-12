using Microsoft.AspNetCore.Mvc;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DailyClosingsController : ControllerBase
{
    private readonly IDailyClosingRepository _repo;
    public DailyClosingsController(IDailyClosingRepository repo) => _repo = repo;

    [HttpGet] public async Task<IActionResult> GetHistory() => Ok(await _repo.GetHistoryAsync());
    [HttpGet("has-closed-today")] public async Task<IActionResult> HasClosedToday() => Ok(await _repo.HasClosedTodayAsync());
    [HttpPost] public async Task<IActionResult> Create([FromBody] DailyClosing d) => Ok(await _repo.CreateAsync(d));
}
