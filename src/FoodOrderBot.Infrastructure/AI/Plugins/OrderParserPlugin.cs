using System.Text.Json;
using FoodOrderBot.Application.AI;
using FoodOrderBot.Application.Parsing;
using FoodOrderBot.Domain.Entities;
using FoodOrderBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace FoodOrderBot.Infrastructure.AI.Plugins;

/// <summary>
/// Parse đơn hàng từ tin nhắn tự nhiên — dùng model 70b (chính xác nhất).
/// Refactor từ MessageParserService, thêm conversation context.
/// </summary>
public class OrderParserPlugin(
    AppDbContext db,
    AiKernelFactory kernelFactory,
    ILogger<OrderParserPlugin> logger)
{
    private static readonly string PromptTemplate = LoadPrompt("order_parser.txt");

    public async Task<ParseResultDto> ParseAsync(
        string message,
        Guid shopId,
        List<ConversationMessage> history,
        CancellationToken ct = default)
    {
        try
        {
            // Lấy menu từ DB
            var menuItems = await db.MenuItems
                .Where(m => m.ShopId == shopId && m.IsAvailable)
                .Select(m => new { id = m.Id, name = m.Name, price = m.Price })
                .ToListAsync(ct);

            var menuJson = JsonSerializer.Serialize(menuItems);
            var historyText = FormatHistory(history);

            var prompt = PromptTemplate
                .Replace("{MESSAGE}", message)
                .Replace("{MENU_JSON}", menuJson)
                .Replace("{CONVERSATION_HISTORY}", historyText);

            var kernel = kernelFactory.GetKernel("OrderParser");
            var chatService = kernel.GetRequiredService<IChatCompletionService>();

            var chat = new ChatHistory();
            chat.AddUserMessage(prompt);

            var response = await chatService.GetChatMessageContentAsync(chat, cancellationToken: ct);
            var json = response.Content?.Trim() ?? "";

            logger.LogDebug("[OrderParser] Model={Model} | Input={Input} | Response={Json}",
                kernelFactory.GetModelId("OrderParser"),
                message[..Math.Min(50, message.Length)],
                json[..Math.Min(100, json.Length)]);

            return ParseResponse(json);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[OrderParser] Lỗi parse đơn hàng: {Message}", message);
            return new ParseResultDto
            {
                Confidence = 0,
                UnclearParts = [$"Lỗi AI: {ex.Message}"]
            };
        }
    }

    private static ParseResultDto ParseResponse(string json)
    {
        try
        {
            // Strip markdown code block nếu model trả về
            if (json.Contains("```"))
            {
                var start = json.IndexOf('{');
                var end = json.LastIndexOf('}');
                if (start >= 0 && end > start) json = json[start..(end + 1)];
            }

            using var doc = JsonDocument.Parse(json);
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

    private static string FormatHistory(List<ConversationMessage> history)
    {
        if (history.Count == 0) return "(không có)";
        return string.Join("\n", history.Select(h => $"[{h.Role}]: {h.Content}"));
    }

    private static string? GetNullableString(JsonElement element, string property)
        => element.TryGetProperty(property, out var val) && val.ValueKind != JsonValueKind.Null
            ? val.GetString() : null;

    private static string LoadPrompt(string fileName)
    {
        var dir = Path.GetDirectoryName(typeof(OrderParserPlugin).Assembly.Location)!;
        var path = Path.Combine(dir, "AI", "Prompts", fileName);
        return File.Exists(path) ? File.ReadAllText(path) : "";
    }
}
