using Microsoft.AspNetCore.Mvc;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockAdjustmentsController : ControllerBase
{
    private readonly IStockAdjustmentRepository _repo;
    public StockAdjustmentsController(IStockAdjustmentRepository repo) => _repo = repo;

    [HttpGet] public async Task<IActionResult> GetRecent([FromQuery] int limit = 50) => Ok(await _repo.GetRecentAsync(limit));
    [HttpPost] public async Task<IActionResult> Create([FromBody] StockAdjustment s) => Ok(await _repo.CreateAsync(s));
}
