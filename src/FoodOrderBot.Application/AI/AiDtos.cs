using FoodOrderBot.Application.Parsing;

namespace FoodOrderBot.Application.AI;

/// <summary>
/// Request gửi vào AI Orchestrator.
/// </summary>
/// <param name="FbSenderId">Facebook Sender ID của khách</param>
/// <param name="ShopId">ID của quán</param>
/// <param name="Content">Nội dung tin nhắn</param>
/// <param name="Source">Nguồn tin nhắn: "Messenger" | "Comment" | "Test"</param>
public record AiRequest(
    string FbSenderId,
    Guid ShopId,
    string Content,
    string Source);

/// <summary>
/// Kết quả trả về từ AI Orchestrator sau khi xử lý tin nhắn.
/// </summary>
public record AiResponse
{
    /// <summary>Ý định đã phân loại</summary>
    public AiIntent Intent { get; init; }

    /// <summary>Kết quả parse đơn hàng — chỉ có khi Intent = PlaceOrder</summary>
    public ParseResultDto? ParseResult { get; init; }

    /// <summary>Tin nhắn reply gửi lại cho khách qua Messenger</summary>
    public string ReplyText { get; init; } = string.Empty;

    /// <summary>Gợi ý upsell — chỉ có khi Intent = PlaceOrder và confidence đủ cao</summary>
    public List<UpsellSuggestion> Suggestions { get; init; } = [];

    /// <summary>Kết quả phân tích cảm xúc — chỉ có khi Intent = Complaint</summary>
    public SentimentResult? Sentiment { get; init; }

    /// <summary>Confidence của order parsing (0.0 – 1.0)</summary>
    public double Confidence { get; init; }
}

/// <summary>
/// Gợi ý bán thêm sau khi khách đặt đơn.
/// </summary>
public record UpsellSuggestion(
    string ItemName,
    decimal Price,
    string Reason);

/// <summary>
/// Kết quả phân tích cảm xúc tin nhắn.
/// </summary>
/// <param name="Label">"positive" | "neutral" | "negative"</param>
/// <param name="Score">Confidence score 0.0 – 1.0</param>
/// <param name="NeedsAttention">true → hiện cảnh báo trên Dashboard cho chủ quán</param>
public record SentimentResult(
    string Label,
    double Score,
    bool NeedsAttention);
