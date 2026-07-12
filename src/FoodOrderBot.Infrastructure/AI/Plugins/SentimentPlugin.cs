using System.Text.Json;
using FoodOrderBot.Application.AI;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace FoodOrderBot.Infrastructure.AI.Plugins;

/// <summary>
/// Phân tích cảm xúc tin nhắn khách — đánh dấu những tin cần chủ quán chú ý.
/// </summary>
public class SentimentPlugin(
    AiKernelFactory kernelFactory,
    ILogger<SentimentPlugin> logger)
{
    private static readonly string PromptTemplate = LoadPrompt("sentiment_analyzer.txt");

    public async Task<SentimentResult> AnalyzeAsync(
        string message,
        CancellationToken ct = default)
    {
        try
        {
            var prompt = PromptTemplate.Replace("{MESSAGE}", message);

            var kernel = kernelFactory.GetKernel("Sentiment");
            var chatService = kernel.GetRequiredService<IChatCompletionService>();

            var chat = new ChatHistory();
            chat.AddUserMessage(prompt);

            var response = await chatService.GetChatMessageContentAsync(chat, cancellationToken: ct);
            var json = response.Content?.Trim() ?? "{}";

            logger.LogDebug("[SentimentPlugin] Response={Json}", json);

            return ParseSentiment(json);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[SentimentPlugin] Lỗi phân tích sentiment — dùng default neutral");
            return new SentimentResult("neutral", 0.5, false);
        }
    }

    private static SentimentResult ParseSentiment(string json)
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
            var root = doc.RootElement;

            var label = root.TryGetProperty("label", out var l) ? l.GetString() ?? "neutral" : "neutral";
            var score = root.TryGetProperty("score", out var s) ? s.GetDouble() : 0.5;
            var needsAttention = root.TryGetProperty("needsAttention", out var n) && n.GetBoolean();

            return new SentimentResult(label, score, needsAttention);
        }
        catch
        {
            return new SentimentResult("neutral", 0.5, false);
        }
    }

    private static string LoadPrompt(string fileName)
    {
        var dir = Path.GetDirectoryName(typeof(SentimentPlugin).Assembly.Location)!;
        var path = Path.Combine(dir, "AI", "Prompts", fileName);
        return File.Exists(path) ? File.ReadAllText(path) : "Phân tích sentiment: {MESSAGE}\nJSON: {\"label\":\"neutral\",\"score\":0.5,\"needsAttention\":false}";
    }
}
