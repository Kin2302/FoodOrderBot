namespace FoodOrderBot.Application.Parsing;

/// <summary>
/// DTO kết quả phân tích của AI — ánh xạ 1-1 với JSON mà Groq trả về
/// </summary>
public class ParseResultDto
{
    public List<ParsedOrderItem> Items { get; set; } = [];
    public string ReceiverName { get; set; } = string.Empty;
    public string ReceiverPhone { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public float Confidence { get; set; }

    /// <summary>
    /// Những phần AI không chắc chắn — hiển thị cho chủ quán biết cần kiểm tra
    /// </summary>
    public List<string> UnclearParts { get; set; } = [];
}

public class ParsedOrderItem
{
    public Guid MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? Note { get; set; }
}
