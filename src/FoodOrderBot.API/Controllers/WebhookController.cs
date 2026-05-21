using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FoodOrderBot.API.BackgroundServices;
using Microsoft.AspNetCore.Mvc;

namespace FoodOrderBot.API.Controllers;

/// <summary>
/// Nhận và xác thực Facebook Webhook events.
/// Quan trọng: phải trả HTTP 200 < 5 giây, mọi xử lý nặng đẩy vào background queue.
/// </summary>
[ApiController]
[Route("api/webhook")]
public class WebhookController(
    WebhookTaskQueue queue,
    IConfiguration config,
    ILogger<WebhookController> logger) : ControllerBase
{
    /// <summary>
    /// GET — Facebook gọi để xác minh webhook khi setup lần đầu (Hub Challenge)
    /// </summary>
    [HttpGet]
    public IActionResult Verify(
        [FromQuery(Name = "hub.mode")] string mode,
        [FromQuery(Name = "hub.verify_token")] string verifyToken,
        [FromQuery(Name = "hub.challenge")] string challenge)
    {
        var expectedToken = config["Facebook:VerifyToken"];
        if (mode == "subscribe" && verifyToken == expectedToken)
        {
            logger.LogInformation("Facebook Webhook verified successfully.");
            return Ok(challenge);  // Trả lại challenge để FB xác nhận
        }

        logger.LogWarning("Webhook verification failed. Token mismatch.");
        return Forbid();
    }

    /// <summary>
    /// POST — Facebook gửi events thực tế (tin nhắn mới, comment mới)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Receive([FromBody] JsonElement payload)
    {
        // 1. Xác thực chữ ký X-Hub-Signature-256
        if (!ValidateSignature(Request))
        {
            logger.LogWarning("Invalid webhook signature — possible spoofing attempt.");
            return Forbid();
        }

        // 2. Enqueue ngay để trả 200 OK nhanh nhất có thể (< 500ms)
        try
        {
            await EnqueueEventsAsync(payload);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi khi enqueue webhook payload.");
            // Vẫn trả 200 để FB không retry lặp lại
        }

        return Ok();  // Facebook cần 200, không quan tâm body
    }

    private async Task EnqueueEventsAsync(JsonElement payload)
    {
        // Hardcode shopId cho MVP 1 quán — Sprint 6 sẽ resolve từ fb_page_id
        var shopId = config.GetValue<Guid>("Shop:DefaultShopId");

        if (!payload.TryGetProperty("entry", out var entries)) return;

        foreach (var entry in entries.EnumerateArray())
        {
            // --- Messenger messages ---
            if (entry.TryGetProperty("messaging", out var messagingArray))
            {
                foreach (var messaging in messagingArray.EnumerateArray())
                {
                    if (!messaging.TryGetProperty("message", out var msg)) continue;
                    if (!msg.TryGetProperty("text", out var textEl)) continue;

                    var senderId = messaging.GetProperty("sender").GetProperty("id").GetString() ?? "";
                    var messageId = msg.GetProperty("mid").GetString() ?? Guid.NewGuid().ToString();
                    var text = textEl.GetString() ?? "";

                    await queue.EnqueueAsync(new WebhookTask(
                        FbSenderId: senderId,
                        FbMessageId: messageId,
                        Content: text,
                        Source: "messenger",
                        FbPostId: null,
                        FbCommentId: null,
                        ShopId: shopId
                    ));

                    logger.LogInformation("Enqueued Messenger msg: {MsgId}", messageId);
                }
            }

            // --- Feed comments ---
            if (entry.TryGetProperty("changes", out var changes))
            {
                foreach (var change in changes.EnumerateArray())
                {
                    if (change.GetProperty("field").GetString() != "feed") continue;
                    var value = change.GetProperty("value");
                    if (value.GetProperty("item").GetString() != "comment") continue;
                    if (value.GetProperty("verb").GetString() != "add") continue;

                    var senderId = value.GetProperty("from").GetProperty("id").GetString() ?? "";
                    var commentId = value.GetProperty("comment_id").GetString() ?? Guid.NewGuid().ToString();
                    var postId = value.GetProperty("post_id").GetString();
                    var text = value.GetProperty("message").GetString() ?? "";

                    await queue.EnqueueAsync(new WebhookTask(
                        FbSenderId: senderId,
                        FbMessageId: commentId,
                        Content: text,
                        Source: "comment",
                        FbPostId: postId,
                        FbCommentId: commentId,
                        ShopId: shopId
                    ));

                    logger.LogInformation("Enqueued Comment: {CommentId}", commentId);
                }
            }
        }
    }

    private bool ValidateSignature(HttpRequest request)
    {
        var appSecret = config["Facebook:AppSecret"];
        if (string.IsNullOrEmpty(appSecret)) return true;  // Dev mode: skip nếu chưa cấu hình

        if (!request.Headers.TryGetValue("X-Hub-Signature-256", out var signature)) return false;

        request.EnableBuffering();
        request.Body.Position = 0;
        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        var body = reader.ReadToEnd();
        request.Body.Position = 0;

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
        var expected = "sha256=" + Convert.ToHexString(hash).ToLower();

        return signature.ToString() == expected;
    }
}
