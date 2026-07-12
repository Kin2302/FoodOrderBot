using FoodOrderBot.Application.AI;
using FoodOrderBot.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodOrderBot.API.Controllers;

/// <summary>
/// Endpoint test AI trực tiếp — hữu ích cho demo và debugging.
/// </summary>
[ApiController]
[Route("api/ai")]
[Authorize]
public class AiController(IAiOrchestrator orchestrator) : ControllerBase
{
    /// <summary>
    /// Test AI pipeline: gửi text → nhận đầy đủ kết quả (intent, parse, reply, sentiment, upsell).
    /// POST /api/ai/test
    /// </summary>
    [HttpPost("test")]
    public async Task<ActionResult<AiResponse>> Test(
        [FromBody] AiTestRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest("Text không được để trống.");

        var aiRequest = new AiRequest(
            FbSenderId: request.FbSenderId ?? $"test-{Guid.NewGuid():N}",
            ShopId: request.ShopId,
            Content: request.Text,
            Source: "Test");

        var response = await orchestrator.ProcessMessageAsync(aiRequest, ct);
        return Ok(response);
    }
}

/// <summary>Request body cho endpoint test AI.</summary>
public record AiTestRequest(
    string Text,
    Guid ShopId,
    /// <summary>Tuỳ chọn — dùng để test conversation context (cùng sender = có context)</summary>
    string? FbSenderId = null);
