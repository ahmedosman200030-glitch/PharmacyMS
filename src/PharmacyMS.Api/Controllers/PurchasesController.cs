using Microsoft.AspNetCore.Mvc;
using PharmacyMS.Application.Interfaces.Repositories;
using PharmacyMS.Domain.Entities;

namespace PharmacyMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PurchasesController : ControllerBase
{
    private readonly IPurchaseRepository _repo;
    public PurchasesController(IPurchaseRepository repo) => _repo = repo;

    [HttpGet] public async Task<IActionResult> GetAll() => Ok(await _repo.GetAllAsync());
    [HttpGet("{id}")] public async Task<IActionResult> GetById(int id) => Ok(await _repo.GetByIdAsync(id));
    [HttpGet("spend-by-supplier")] public async Task<IActionResult> SpendBySupplier() => Ok(await _repo.GetSpendBySupplierAsync());
    [HttpGet("spend-by-medicine")] public async Task<IActionResult> SpendByMedicine() => Ok(await _repo.GetSpendByMedicineAsync());
    [HttpGet("total-spend")] public async Task<IActionResult> TotalSpend() => Ok(await _repo.GetTotalSpendAsync());
    [HttpPost] public async Task<IActionResult> Create([FromBody] Purchase p) => Ok(await _repo.CreatePurchaseAsync(p));
    [HttpPost("{id}/payment")] public async Task<IActionResult> Payment(int id, [FromBody] PaymentRequest req) { await _repo.RecordPaymentAsync(id, req.Amount); return Ok(); }
}

public record PaymentRequest(decimal Amount);
