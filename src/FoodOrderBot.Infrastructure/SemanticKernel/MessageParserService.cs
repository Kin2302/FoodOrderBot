using FoodOrderBot.Application.Contracts;
using FoodOrderBot.Application.Parsing;
using FoodOrderBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;

namespace FoodOrderBot.Infrastructure.SemanticKernel;

/// <summary>
/// Phân tích tin nhắn đặt đồ ăn bằng Groq AI (qua Semantic Kernel).
/// </summary>
public class MessageParserService(
    AppDbContext db,
    IConfiguration config,
    ILogger<MessageParserService> logger) : IMessageParser
{
    private const string SystemPromptTemplate = """
        Bạn là AI phân tích đơn đặt đồ ăn từ tin nhắn tiếng Việt.
        
        Nhiệm vụ: Từ tin nhắn của khách, trích xuất thông tin và trả về JSON THUẦN TÚY (không markdown, không ```, chỉ JSON).
        
        Schema JSON:
        {
          "items": [
            {
              "menuItemId": "string | null",
              "name": "string",
              "quantity": number,
              "note": "string | null"
            }
          ],
          "receiverName": "string | null",
          "receiverPhone": "string | null",
          "deliveryAddress": "string | null",
          "confidence": number (0.0-1.0),
          "unclearParts": ["string"]
        }
        
        Quy tắc confidence:
        - 0.0: không phải đặt hàng
        - 0.5-0.79: thiếu SĐT hoặc địa chỉ
        - 0.8-1.0: đủ thông tin xử lý
        
        Ví dụ 1:
        Input: "cho e 1 cơm sườn ít cơm nhiều chả ship HUTECH khu E, sđt 0901234567"
        Output: {"items":[{"menuItemId":null,"name":"Cơm tấm sườn chả","quantity":1,"note":"ít cơm nhiều chả"}],"receiverName":null,"receiverPhone":"0901234567","deliveryAddress":"HUTECH khu E","confidence":0.92,"unclearParts":[]}
        
        Ví dụ 2:
        Input: "2 bún bò + 1 phở tái, giao Q3"
        Output: {"items":[{"menuItemId":null,"name":"Bún bò Huế","quantity":2,"note":null},{"menuItemId":null,"name":"Phở bò tái","quantity":1,"note":"tái"}],"receiverName":null,"receiverPhone":null,"deliveryAddress":"Quận 3","confidence":0.72,"unclearParts":["Thiếu số điện thoại"]}
        
        Ví dụ 3:
        Input: "shop ơi ngon quá"
        Output: {"items":[],"receiverName":null,"receiverPhone":null,"deliveryAddress":null,"confidence":0.0,"unclearParts":["Không phải tin nhắn đặt hàng"]}
        
        Menu hiện tại:
        {MENU_JSON}
        """;

    public async Task<ParseResultDto> ParseAsync(string rawText, Guid shopId, CancellationToken ct = default)
    {
        try
        {
            var apiKey = config["Groq:ApiKey"]
                ?? throw new InvalidOperationException("Groq:ApiKey chưa cấu hình.");
            var modelId = config["Groq:ModelId"] ?? "llama-3.3-70b-versatile";

            // Lấy menu từ DB
            var menuItems = await db.MenuItems
                .Where(m => m.ShopId == shopId && m.IsAvailable)
                .Select(m => new { id = m.Id, name = m.Name, price = m.Price })
                .ToListAsync(ct);

            var menuJson = JsonSerializer.Serialize(menuItems);
            var systemPrompt = SystemPromptTemplate.Replace("{MENU_JSON}", menuJson);

            // Build SK Kernel với Groq endpoint 
            var kernel = Kernel.CreateBuilder()
                .AddOpenAIChatCompletion(
                    modelId: modelId,
                    apiKey: apiKey,
                    endpoint: new Uri("https://api.groq.com/openai/v1"))
                .Build();

            var chatService = kernel.GetRequiredService<IChatCompletionService>();
            var history = new ChatHistory();
            history.AddSystemMessage(systemPrompt);
            history.AddUserMessage(rawText);

            var response = await chatService.GetChatMessageContentAsync(history, cancellationToken: ct);
            var jsonText = response.Content?.Trim() ?? "";

            logger.LogDebug("Groq response for '{Text}': {Json}", rawText[..Math.Min(50, rawText.Length)], jsonText);

            return ParseResponse(jsonText);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AI parse failed for text: {Text}", rawText);
            return new ParseResultDto
            {
                Confidence = 0,
                UnclearParts = [$"Lỗi AI: {ex.Message}"]
            };
        }
    }

    private static ParseResultDto ParseResponse(string jsonText)
    {
        try
        {
            // Strip markdown code block nếu model vẫn trả về
            if (jsonText.Contains("```"))
            {
                var start = jsonText.IndexOf('{');
                var end = jsonText.LastIndexOf('}');
                if (start >= 0 && end > start)
                    jsonText = jsonText[start..(end + 1)];
            }

            using var doc = JsonDocument.Parse(jsonText);
            var root = doc.RootElement;

            var dto = new ParseResultDto
            {
                Confidence = root.TryGetProperty("confidence", out var conf) ? conf.GetDouble() : 0,
                ReceiverName = GetNullableString(root, "receiverName"),
                ReceiverPhone = GetNullableString(root, "receiverPhone"),
                DeliveryAddress = GetNullableString(root, "deliveryAddress"),
                UnclearParts = root.TryGetProperty("unclearParts", out var up)
                    ? [.. up.EnumerateArray().Select(x => x.GetString()!)]
                    : [],
            };

            if (root.TryGetProperty("items", out var items))
            {
                dto.Items = [.. items.EnumerateArray().Select(item => new ParsedOrderItem
                {
                    MenuItemId = item.TryGetProperty("menuItemId", out var mid) && mid.ValueKind != JsonValueKind.Null
                        ? (Guid.TryParse(mid.GetString(), out var g) ? g : null) : null,
                    Name = item.TryGetProperty("name", out var n) ? n.GetString()! : "Unknown",
                    Quantity = item.TryGetProperty("quantity", out var q) ? q.GetInt32() : 1,
                    Note = GetNullableString(item, "note"),
                })];
            }

            return dto;
        }
        catch (Exception ex)
        {
            return new ParseResultDto
            {
                Confidence = 0,
                UnclearParts = [$"Parse error: {ex.Message}"]
            };
        }
    }

    private static string? GetNullableString(JsonElement element, string property)
        => element.TryGetProperty(property, out var val) && val.ValueKind != JsonValueKind.Null
            ? val.GetString() : null;
}
