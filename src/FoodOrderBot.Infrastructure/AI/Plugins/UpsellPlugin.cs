using System.Text.Json;
using FoodOrderBot.Application.AI;
using FoodOrderBot.Application.Parsing;
using FoodOrderBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace FoodOrderBot.Infrastructure.AI.Plugins;

/// <summary>
/// Gợi ý bán thêm (upsell) sau khi khách đặt đơn thành công.
/// </summary>
public class UpsellPlugin(
    AppDbContext db,
    AiKernelFactory kernelFactory,
    ILogger<UpsellPlugin> logger)
{
    private static readonly string PromptTemplate = LoadPrompt("upsell_suggest.txt");

    public async Task<List<UpsellSuggestion>> SuggestAsync(
        ParseResultDto parsedOrder,
        Guid shopId,
        CancellationToken ct = default)
    {
        // Chỉ upsell khi có ít nhất 1 món
        if (parsedOrder.Items.Count == 0) return [];

        try
        {
            var menuItems = await db.MenuItems
                .Where(m => m.ShopId == shopId && m.IsAvailable)
                .Select(m => new { m.Id, m.Name, m.Price })
                .ToListAsync(ct);

            var orderedNames = parsedOrder.Items.Select(i => i.Name).ToList();
            var menuJson = JsonSerializer.Serialize(menuItems);
            var orderJson = JsonSerializer.Serialize(orderedNames);

            var prompt = PromptTemplate
                .Replace("{ORDER_ITEMS}", orderJson)
                .Replace("{MENU_JSON}", menuJson);

            var kernel = kernelFactory.GetKernel("Upsell");
            var chatService = kernel.GetRequiredService<IChatCompletionService>();

            var chat = new ChatHistory();
            chat.AddUserMessage(prompt);

            var response = await chatService.GetChatMessageContentAsync(chat, cancellationToken: ct);
            var json = response.Content?.Trim() ?? "{}";

            return ParseSuggestions(json);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[UpsellPlugin] Lỗi gợi ý upsell — bỏ qua, không ảnh hưởng flow chính");
            return [];
        }
    }

    private static List<UpsellSuggestion> ParseSuggestions(string json)
    {
        try
        {
            if (json.Contains("```"))
            {
                var start = json.IndexOf('{');
                var end = json.LastIndexOf('}');
                if (start >= 0 && end > start) json = json[start..(end + 1)];
            }

            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("suggestions", out var arr)) return [];

            return [.. arr.EnumerateArray().Take(2).Select(s => new UpsellSuggestion(
                ItemName: s.TryGetProperty("itemName", out var n) ? n.GetString() ?? "" : "",
                Price: s.TryGetProperty("price", out var p) ? (decimal)p.GetDouble() : 0,
                Reason: s.TryGetProperty("reason", out var r) ? r.GetString() ?? "" : ""
            ))];
        }
        catch
        {
            return [];
        }
    }

    private static string LoadPrompt(string fileName)
    {
        var dir = Path.GetDirectoryName(typeof(UpsellPlugin).Assembly.Location)!;
        var path = Path.Combine(dir, "AI", "Prompts", fileName);
        return File.Exists(path) ? File.ReadAllText(path) : "";
    }
}
