namespace FoodOrderBot.Application.Analytics;

public class ComplaintDto
{
    public Guid Id { get; set; }
    public string FbSenderId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? SentimentLabel { get; set; }
    public double? SentimentScore { get; set; }
    public bool NeedsAttention { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ConversationHistoryDto
{
    public string FbSenderId { get; set; } = string.Empty;
    public List<ConversationMessageDto> Messages { get; set; } = [];
}

public record ConversationMessageDto(
    string Role,
    string Content,
    string? Intent,
    DateTime CreatedAt);
