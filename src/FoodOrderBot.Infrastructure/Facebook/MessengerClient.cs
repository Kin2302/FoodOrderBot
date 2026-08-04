using System.Text;
using System.Text.Json;
using FoodOrderBot.Application.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FoodOrderBot.Infrastructure.Facebook;

/// <summary>
/// Gọi Facebook Send API để gửi tin nhắn cho khách qua Messenger.
/// Dùng HttpClient inject từ IHttpClientFactory (qua AddHttpClient).
/// </summary>
public class MessengerClient(
    HttpClient httpClient,
    IConfiguration config,
    ILogger<MessengerClient> logger) : IMessengerReply
{
    private const string GraphApiVersion = "v21.0";
    private const string BaseUrl = $"https://graph.facebook.com/{GraphApiVersion}/me/messages";

    public async Task SendTextAsync(string fbSenderId, 
                                    string message, 
                                    string pageAccessToken, 
                                    CancellationToken ct = default)
    {
        var payload = new
        {
            recipient = new { id = fbSenderId },
            messaging_type = "RESPONSE",
            message = new { text = message }
        };

        await SendAsync(payload, pageAccessToken, ct);
    }

    public async Task SendTrackingLinkAsync(string fbSenderId, 
                                            string orderId, 
                                            string trackingToken, 
                                            string pageAccessToken, 
                                            CancellationToken ct = default)
    {
        var frontendBaseUrl = config["Facebook:FrontendBaseUrl"] ?? "http://localhost:5173";
        var trackingUrl = $"{frontendBaseUrl}/track/{trackingToken}";

        // Gửi dạng button template để khách bấm vào link tracking
        var payload = new
        {
            recipient = new { id = fbSenderId },
            messaging_type = "RESPONSE",
            message = new
            {
                attachment = new
                {
                    type = "template",
                    payload = new
                    {
                        template_type = "button",
                        text = "🎉 Đơn hàng của bạn đã được tiếp nhận!\nBấm nút bên dưới để theo dõi trạng thái đơn hàng:",
                        buttons = new[]
                        {
                            new
                            {
                                type = "web_url",
                                url = trackingUrl,
                                title = "📦 Theo dõi đơn hàng"
                            }
                        }
                    }
                }
            }
        };

        await SendAsync(payload, pageAccessToken, ct);
    }

    private async Task SendAsync(object payload, string pageAccessToken, CancellationToken ct)
    {
        try
        {
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var url = $"{BaseUrl}?access_token={pageAccessToken}";
            var response = await httpClient.PostAsync(url, content, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                logger.LogError(
                    "Facebook Send API failed: {StatusCode} — {Body}",
                    response.StatusCode, errorBody);
            }
            else
            {
                logger.LogInformation("Messenger reply sent successfully.");
            }
        }
        catch (Exception ex)
        {
            // Không throw — worker không được crash vì lỗi reply
            logger.LogError(ex, "Lỗi khi gửi Messenger reply.");
        }
    }
}
