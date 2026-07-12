using FoodOrderBot.Application.AI;
using FoodOrderBot.Application.Contracts;
using FoodOrderBot.Domain.Entities;
using FoodOrderBot.Domain.Interfaces;
using FoodOrderBot.Infrastructure.AI.Plugins;
using Microsoft.Extensions.Logging;

namespace FoodOrderBot.Infrastructure.AI;

/// <summary>
/// Điều phối toàn bộ AI pipeline:
/// 1. Load conversation context
/// 2. Classify intent (8b)
/// 3. Route đến plugin phù hợp
/// 4. Lưu hội thoại vào DB
/// 5. Trả AiResponse cho Worker
/// </summary>
public class AiOrchestrator(
    IntentClassifierPlugin intentPlugin,
    OrderParserPlugin orderPlugin,
    ChatBotPlugin chatPlugin,
    UpsellPlugin upsellPlugin,
    SentimentPlugin sentimentPlugin,
    IConversationRepository conversationRepo,
    ILogger<AiOrchestrator> logger) : IAiOrchestrator
{
    public async Task<AiResponse> ProcessMessageAsync(AiRequest request, CancellationToken ct = default)
    {
        logger.LogInformation("[Orchestrator] Processing: Sender={Sender} | Content={Content}",
            request.FbSenderId, request.Content[..Math.Min(50, request.Content.Length)]);

        // ── 1. Load conversation context (5 tin gần nhất) ──────────────────────
        var history = await conversationRepo.GetRecentBySenderAsync(
            request.FbSenderId, request.ShopId, limit: 5, ct);

        // ── 2. Classify intent ──────────────────────────────────────────────────
        var intent = await intentPlugin.ClassifyAsync(request.Content, history, ct);

        logger.LogInformation("[Orchestrator] Intent={Intent} | Sender={Sender}", intent, request.FbSenderId);

        // ── 3. Route theo intent ────────────────────────────────────────────────
        AiResponse response = intent switch
        {
            AiIntent.PlaceOrder => await HandlePlaceOrderAsync(request, history, ct),
            AiIntent.Complaint  => await HandleComplaintAsync(request, history, ct),
            _                   => await HandleGeneralAsync(intent, request, history, ct),
        };

        // ── 4. Lưu hội thoại vào DB ─────────────────────────────────────────────
        await SaveConversationAsync(request, response, intent, ct);

        return response;
    }

    // ────────────────────────────────────────────────────────────────────────────
    // Handlers
    // ────────────────────────────────────────────────────────────────────────────

    private async Task<AiResponse> HandlePlaceOrderAsync(
        AiRequest request,
        List<ConversationMessage> history,
        CancellationToken ct)
    {
        // Parse đơn hàng (70b model)
        var parseResult = await orderPlugin.ParseAsync(request.Content, request.ShopId, history, ct);

        string replyText;
        List<UpsellSuggestion> suggestions = [];

        if (parseResult.Confidence >= 0.8 && parseResult.Items.Count > 0)
        {
            // Confidence đủ → tạo đơn + upsell
            suggestions = await upsellPlugin.SuggestAsync(parseResult, request.ShopId, ct);

            var upsellText = suggestions.Count > 0
                ? $"\n\n💡 Gợi ý thêm: {string.Join(", ", suggestions.Select(s => $"{s.ItemName} ({s.Price:N0}đ) — {s.Reason}"))}"
                : "";

            replyText = $"✅ Đơn hàng của bạn đã được ghi nhận! Quán sẽ xác nhận sớm nhé.{upsellText}";
        }
        else if (parseResult.Confidence > 0)
        {
            // Confidence thấp → hỏi lại
            var unclear = parseResult.UnclearParts.Count > 0
                ? string.Join(", ", parseResult.UnclearParts)
                : "thông tin đặt hàng";
            replyText = $"Cảm ơn bạn đã nhắn tin! 🙏\nMình chưa rõ: {unclear}.\nBạn vui lòng gửi lại đầy đủ: tên món, số lượng, SĐT và địa chỉ giao hàng nhé!";
        }
        else
        {
            // AI nhận diện sai intent → fallback về chatbot
            replyText = await chatPlugin.GenerateReplyAsync(
                AiIntent.Other, request.Content, request.ShopId, history, ct: ct);
        }

        return new AiResponse
        {
            Intent = AiIntent.PlaceOrder,
            ParseResult = parseResult,
            ReplyText = replyText,
            Suggestions = suggestions,
            Confidence = parseResult.Confidence
        };
    }

    private async Task<AiResponse> HandleComplaintAsync(
        AiRequest request,
        List<ConversationMessage> history,
        CancellationToken ct)
    {
        // Phân tích cảm xúc + tạo reply đồng cảm song song
        var sentimentTask = sentimentPlugin.AnalyzeAsync(request.Content, ct);
        var replyTask = chatPlugin.GenerateReplyAsync(
            AiIntent.Complaint, request.Content, request.ShopId, history, ct: ct);

        await Task.WhenAll(sentimentTask, replyTask);

        var sentiment = await sentimentTask;
        var reply = await replyTask;

        if (sentiment.NeedsAttention)
        {
            logger.LogWarning("[Orchestrator] ⚠️ Khiếu nại cần chú ý! Sender={Sender} | Score={Score}",
                request.FbSenderId, sentiment.Score);
        }

        return new AiResponse
        {
            Intent = AiIntent.Complaint,
            ReplyText = reply,
            Sentiment = sentiment,
            Confidence = 1.0
        };
    }

    private async Task<AiResponse> HandleGeneralAsync(
        AiIntent intent,
        AiRequest request,
        List<ConversationMessage> history,
        CancellationToken ct)
    {
        var reply = await chatPlugin.GenerateReplyAsync(
            intent, request.Content, request.ShopId, history, ct: ct);

        return new AiResponse
        {
            Intent = intent,
            ReplyText = reply,
            Confidence = 1.0
        };
    }

    // ────────────────────────────────────────────────────────────────────────────
    // Lưu hội thoại
    // ────────────────────────────────────────────────────────────────────────────

    private async Task SaveConversationAsync(
        AiRequest request,
        AiResponse response,
        AiIntent intent,
        CancellationToken ct)
    {
        try
        {
            // Lưu tin của user
            await conversationRepo.AddAsync(new ConversationMessage
            {
                Id = Guid.NewGuid(),
                FbSenderId = request.FbSenderId,
                ShopId = request.ShopId,
                Role = "User",
                Content = request.Content,
                Intent = intent.ToString(),
                CreatedAt = DateTime.UtcNow
            }, ct);

            // Lưu reply của AI (nếu có)
            if (!string.IsNullOrEmpty(response.ReplyText))
            {
                await conversationRepo.AddAsync(new ConversationMessage
                {
                    Id = Guid.NewGuid(),
                    FbSenderId = request.FbSenderId,
                    ShopId = request.ShopId,
                    Role = "Assistant",
                    Content = response.ReplyText,
                    Intent = null,
                    CreatedAt = DateTime.UtcNow
                }, ct);
            }

            await conversationRepo.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            // Không throw — lỗi lưu hội thoại không được làm hỏng flow chính
            logger.LogError(ex, "[Orchestrator] Lỗi lưu conversation history");
        }
    }
}
