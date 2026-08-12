using Microsoft.AspNetCore.Mvc;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SalesController : ControllerBase
{
    private readonly ISaleRepository _repo;
    public SalesController(ISaleRepository repo) => _repo = repo;

    [HttpGet] public async Task<IActionResult> GetAll() => Ok(await _repo.GetAllAsync());
    [HttpGet("range")] public async Task<IActionResult> GetByRange([FromQuery] DateTime from, [FromQuery] DateTime to) => Ok(await _repo.GetByDateRangeAsync(from, to));
    [HttpGet("invoice/{number}")] public async Task<IActionResult> ByInvoice(string number) => Ok(await _repo.GetByInvoiceAsync(number));
    [HttpGet("credit")] public async Task<IActionResult> GetCredit() => Ok(await _repo.GetCreditSalesAsync());
    [HttpPost] public async Task<IActionResult> Create([FromBody] Sale sale) => Ok(await _repo.CreateSaleAsync(sale));
    [HttpPost("{id}/payment")] public async Task<IActionResult> AddPayment(int id, [FromBody] PaymentRequest req) { await _repo.RecordPaymentAsync(id, req.Amount); return Ok(); }
}

