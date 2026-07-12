namespace FoodOrderBot.Domain.Entities;

/// <summary>
/// Lưu lịch sử hội thoại AI theo từng khách (fbSenderId).
/// Cho phép AI nhớ context qua nhiều tin nhắn liên tiếp.
/// </summary>
public class ConversationMessage
{
    public Guid Id { get; set; }

    /// <summary>Facebook Sender ID của khách</summary>
    public string FbSenderId { get; set; } = string.Empty;

    public Guid ShopId { get; set; }

    /// <summary>"User" | "Assistant" | "System"</summary>
    public string Role { get; set; } = "User";

    public string Content { get; set; } = string.Empty;

    /// <summary>Intent đã classify (PlaceOrder, Greeting, ...) — null nếu là tin của AI</summary>
    public string? Intent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Shop Shop { get; set; } = null!;
}
