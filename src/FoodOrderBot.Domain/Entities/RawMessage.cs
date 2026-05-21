using FoodOrderBot.Domain.Enums;

namespace FoodOrderBot.Domain.Entities;

public class RawMessage
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public string FbSenderId { get; set; } = string.Empty;

    /// <summary>
    /// Unique ID từ Facebook — dùng để deduplication.
    /// Với Messenger: message_id; với Comment: comment_id.
    /// </summary>
    public string FbMessageId { get; set; } = string.Empty;

    public string? FbPostId { get; set; }
    public string? FbCommentId { get; set; }
    public MessageSource Source { get; set; }
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Kết quả AI parse — lưu dạng JSON thô để tái sử dụng / fine-tune sau
    /// </summary>
    public string? ParsedResult { get; set; }  // JSONB in PostgreSQL
    public float? ParseConfidence { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Shop Shop { get; set; } = null!;
    public Order? Order { get; set; }
}
