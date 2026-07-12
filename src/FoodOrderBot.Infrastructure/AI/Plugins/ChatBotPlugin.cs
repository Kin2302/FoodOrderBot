using FoodOrderBot.Application.AI;
using FoodOrderBot.Domain.Entities;
using FoodOrderBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace FoodOrderBot.Infrastructure.AI.Plugins;

/// <summary>
/// Tạo reply thông minh cho các intent không phải đặt hàng:
/// Greeting, AskMenu, AskOrderStatus, Compliment, Other.
/// </summary>
public class ChatBotPlugin(
    AppDbContext db,
    AiKernelFactory kernelFactory,
    ILogger<ChatBotPlugin> logger)
{
    private static readonly string PromptTemplate = LoadPrompt("chatbot_reply.txt");

    public async Task<string> GenerateReplyAsync(
        AiIntent intent,
        string message,
        Guid shopId,
        List<ConversationMessage> history,
        string? trackingInfo = null,
        CancellationToken ct = default)
    {
        try
        {
            // Lấy thông tin menu ngắn gọn cho shop
            var menuItems = await db.MenuItems
                .Where(m => m.ShopId == shopId && m.IsAvailable)
                .OrderBy(m => m.Name)
                .Select(m => new { m.Name, m.Price })
                .ToListAsync(ct);

            var shop = await db.Shops.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == shopId, ct);

            var shopInfo = $"Tên quán: {shop?.Name ?? "Quán ăn"}\nMenu: " +
                           string.Join(", ", menuItems.Select(m => $"{m.Name} ({m.Price:N0}đ)"));

            var historyText = history.Count > 0
                ? string.Join("\n", history.Select(h => $"[{h.Role}]: {h.Content}"))
                : "(không có)";

            var prompt = PromptTemplate
                .Replace("{INTENT}", intent.ToString())
                .Replace("{MESSAGE}", message)
                .Replace("{SHOP_INFO}", shopInfo)
                .Replace("{CONVERSATION_HISTORY}", historyText)
                .Replace("{TRACKING_INFO}", trackingInfo ?? "Không có thông tin tracking");

            var kernel = kernelFactory.GetKernel("ChatBot");
            var chatService = kernel.GetRequiredService<IChatCompletionService>();

            var chat = new ChatHistory();
            chat.AddUserMessage(prompt);

            var response = await chatService.GetChatMessageContentAsync(chat, cancellationToken: ct);
            var reply = response.Content?.Trim() ?? DefaultReply(intent);

            logger.LogDebug("[ChatBot] Intent={Intent} | Reply={Reply}",
                intent, reply[..Math.Min(80, reply.Length)]);

            return reply;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[ChatBot] Lỗi tạo reply cho intent {Intent}", intent);
            return DefaultReply(intent);
        }
    }

    private static string DefaultReply(AiIntent intent) => intent switch
    {
        AiIntent.Greeting => "Chào bạn! 👋 Cảm ơn đã liên hệ quán. Bạn muốn xem menu hay đặt món gì không ạ?",
        AiIntent.AskMenu => "Bạn vui lòng nhắn tin trực tiếp để mình tư vấn menu nhé! 🍜",
        AiIntent.Compliment => "Cảm ơn bạn rất nhiều! 🙏 Hẹn gặp lại bạn lần sau nhé!",
        _ => "Cảm ơn bạn đã nhắn tin! 😊 Bạn cần hỗ trợ gì không ạ?"
    };

    private static string LoadPrompt(string fileName)
    {
        var dir = Path.GetDirectoryName(typeof(ChatBotPlugin).Assembly.Location)!;
        var path = Path.Combine(dir, "AI", "Prompts", fileName);
        return File.Exists(path) ? File.ReadAllText(path) : "";
    }
}
