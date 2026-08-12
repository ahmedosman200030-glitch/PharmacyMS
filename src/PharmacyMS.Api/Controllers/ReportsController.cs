using Microsoft.AspNetCore.Mvc;
using PharmacyMS.Application.Interfaces.Repositories;

namespace PharmacyMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IReportRepository _repo;
    public ReportsController(IReportRepository repo) => _repo = repo;

    [HttpGet("revenue")] public async Task<IActionResult> Revenue([FromQuery] DateTime from, [FromQuery] DateTime to) => Ok(await _repo.GetTotalRevenueAsync(from, to));
    [HttpGet("transactions")] public async Task<IActionResult> Transactions([FromQuery] DateTime from, [FromQuery] DateTime to) => Ok(await _repo.GetTotalTransactionsAsync(from, to));
    [HttpGet("purchase-cost")] public async Task<IActionResult> PurchaseCost([FromQuery] DateTime from, [FromQuery] DateTime to) => Ok(await _repo.GetTotalPurchaseCostAsync(from, to));
    [HttpGet("top-medicines")] public async Task<IActionResult> TopMedicines([FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] int top = 10) => Ok(await _repo.GetTopSellingMedicinesAsync(from, to, top));
    [HttpGet("receivables")] public async Task<IActionResult> Receivables() => Ok(await _repo.GetTotalReceivablesAsync());
    [HttpGet("monthly-summary")] public async Task<IActionResult> MonthlySummary([FromQuery] int months = 4) => Ok(await _repo.GetMonthlySalesAndPurchasesAsync(months));
    [HttpGet("stock-reconciliation")] public async Task<IActionResult> StockReconciliation([FromQuery] DateTime from, [FromQuery] DateTime to) => Ok(await _repo.GetMonthlyStockReconciliationAsync(from, to));
    [HttpGet("settings/{key}")] public async Task<IActionResult> GetSetting(string key) => Ok(await _repo.GetSettingAsync(key));
    [HttpPost("settings")] public async Task<IActionResult> SetSetting([FromBody] SettingRequest req) { await _repo.SetSettingAsync(req.Key, req.Value); return Ok(); }
}

public record SettingRequest(string Key, string Value);
