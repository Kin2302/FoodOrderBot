using FoodOrderBot.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodOrderBot.API.Controllers;

/// <summary>
/// Thống kê doanh thu, đơn hàng, AI performance.
/// </summary>
[ApiController]
[Route("api/analytics")]
[Authorize]
public class AnalyticsController(
    IAnalyticsService analyticsService,
    IConfiguration config) : ControllerBase
{
    private Guid DefaultShopId =>
        Guid.Parse(config["Shop:DefaultShopId"]
            ?? throw new InvalidOperationException("Shop:DefaultShopId chưa được cấu hình"));

    // ─── GET /api/analytics/summary?days=30 ──────────────────────────────────

    /// <summary>Tổng hợp doanh thu, đơn hàng, top món, giờ cao điểm.</summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(
        [FromQuery] int days = 30,
        CancellationToken ct = default)
    {
        var summary = await analyticsService.GetSummaryAsync(DefaultShopId, days, ct);
        return Ok(summary);
    }

    // ─── GET /api/analytics/ai-stats?days=30 ─────────────────────────────────

    /// <summary>Thống kê AI: tỷ lệ parse, intent distribution, complaints.</summary>
    [HttpGet("ai-stats")]
    public async Task<IActionResult> GetAiStats(
        [FromQuery] int days = 30,
        CancellationToken ct = default)
    {
        var stats = await analyticsService.GetAiStatsAsync(DefaultShopId, days, ct);
        return Ok(stats);
    }
}
