using FoodOrderBot.Application.Analytics;
using FoodOrderBot.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodOrderBot.API.Controllers;

/// <summary>
/// Quản lý khiếu nại + lịch sử hội thoại từng khách.
/// </summary>
[ApiController]
[Route("api")]
[Authorize]
public class ComplaintController(
    IConversationRepository conversationRepo,
    IConfiguration config) : ControllerBase
{
    private Guid DefaultShopId =>
        Guid.Parse(config["Shop:DefaultShopId"]
            ?? throw new InvalidOperationException("Shop:DefaultShopId chưa được cấu hình"));

    // ─── GET /api/complaints?limit=50 ────────────────────────────────────────

    /// <summary>Danh sách complaints mới nhất.</summary>
    [HttpGet("complaints")]
    public async Task<IActionResult> GetComplaints(
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
    {
        var complaints = await conversationRepo.GetComplaintsByShopAsync(DefaultShopId, limit, ct);

        var dtos = complaints.Select(c => new ComplaintDto
        {
            Id = c.Id,
            FbSenderId = c.FbSenderId,
            Content = c.Content,
            SentimentLabel = c.SentimentLabel,
            SentimentScore = c.SentimentScore,
            NeedsAttention = c.NeedsAttention,
            CreatedAt = c.CreatedAt,
        }).ToList();

        return Ok(dtos);
    }

    // ─── GET /api/conversations/{senderId} ───────────────────────────────────

    /// <summary>Lịch sử hội thoại đầy đủ của 1 khách hàng.</summary>
    [HttpGet("conversations/{senderId}")]
    public async Task<IActionResult> GetConversationHistory(
        string senderId,
        CancellationToken ct = default)
    {
        var messages = await conversationRepo.GetAllBySenderAsync(senderId, DefaultShopId, ct);

        var dto = new ConversationHistoryDto
        {
            FbSenderId = senderId,
            Messages = messages.Select(m => new ConversationMessageDto(
                m.Role,
                m.Content,
                m.Intent,
                m.CreatedAt
            )).ToList(),
        };

        return Ok(dto);
    }
}
