using System.Text.Json;
using FoodOrderBot.Application.AI;
using FoodOrderBot.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace FoodOrderBot.Infrastructure.AI.Plugins;

/// <summary>
/// Phân loại ý định tin nhắn — dùng model 8b (nhanh, tiết kiệm rate limit).
/// </summary>
public class IntentClassifierPlugin(
    AiKernelFactory kernelFactory,
    ILogger<IntentClassifierPlugin> logger)
{
    private static readonly string PromptTemplate = LoadPrompt("intent_classifier.txt");

    public async Task<AiIntent> ClassifyAsync(
        string message,
        List<ConversationMessage> history,
        CancellationToken ct = default)
    {
        try
        {
            var historyText = FormatHistory(history);
            var prompt = PromptTemplate
                .Replace("{MESSAGE}", message)
                .Replace("{CONVERSATION_HISTORY}", historyText);

            var kernel = kernelFactory.GetKernel("IntentClassifier");
            var chatService = kernel.GetRequiredService<IChatCompletionService>();

            var chat = new ChatHistory();
            chat.AddUserMessage(prompt);

            var response = await chatService.GetChatMessageContentAsync(chat, cancellationToken: ct);
            var json = response.Content?.Trim() ?? "{}";

            logger.LogDebug("[IntentClassifier] Model={Model} | Response={Json}",
                kernelFactory.GetModelId("IntentClassifier"), json);

            return ParseIntent(json);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[IntentClassifier] Lỗi phân loại intent cho: {Message}",
                message[..Math.Min(50, message.Length)]);
            return AiIntent.Other;
        }
    }

    private static AiIntent ParseIntent(string json)
    {
        try
        {
            // Strip markdown nếu model vẫn trả về
            if (json.Contains("```"))
            {
                var start = json.IndexOf('{');
                var end = json.LastIndexOf('}');
                if (start >= 0 && end > start) json = json[start..(end + 1)];
            }

            using var doc = JsonDocument.Parse(json);
            var intentStr = doc.RootElement.GetProperty("intent").GetString() ?? "Other";
            return Enum.TryParse<AiIntent>(intentStr, true, out var intent) ? intent : AiIntent.Other;
        }
        catch
        {
            return AiIntent.Other;
        }
    }

    private static string FormatHistory(List<ConversationMessage> history)
    {
        if (history.Count == 0) return "(không có)";
        return string.Join("\n", history.Select(h => $"[{h.Role}]: {h.Content}"));
    }

    private static string LoadPrompt(string fileName)
    {
        var dir = Path.GetDirectoryName(typeof(IntentClassifierPlugin).Assembly.Location)!;
        var path = Path.Combine(dir, "AI", "Prompts", fileName);
        return File.Exists(path) ? File.ReadAllText(path) : FallbackPrompt;
    }

    private const string FallbackPrompt =
        "Phân loại intent tin nhắn sau vào: PlaceOrder, AskMenu, AskOrderStatus, Greeting, Complaint, Compliment, Other.\n" +
        "Trả về JSON: {\"intent\": \"...\"}\nTin nhắn: {MESSAGE}";
}
