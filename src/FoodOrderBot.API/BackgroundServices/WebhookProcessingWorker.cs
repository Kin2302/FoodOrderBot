using FoodOrderBot.API.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace FoodOrderBot.API.BackgroundServices;

/// <summary>
/// Background worker — consume WebhookTaskQueue và xử lý từng event:
/// 1. Dedup check → 2. Lưu RawMessage → 3. Gọi AI parser → 4. Tạo Draft Order → 5. Push SignalR
/// </summary>
public class WebhookProcessingWorker(
    WebhookTaskQueue queue,
    IServiceScopeFactory scopeFactory,
    IHubContext<OrderHub> hubContext,
    ILogger<WebhookProcessingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("WebhookProcessingWorker started.");

        await foreach (var task in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessAsync(task, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Lỗi khi xử lý webhook: FbMessageId={FbMessageId}", task.FbMessageId);
                // Không throw — worker tiếp tục chạy với task tiếp theo
            }
        }
    }

    private async Task ProcessAsync(WebhookTask task, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();

        // TODO Sprint 2: Inject và gọi các services thực tế
        // var rawMsgRepo = scope.ServiceProvider.GetRequiredService<IRawMessageRepository>();
        // var parser = scope.ServiceProvider.GetRequiredService<IMessageParser>();
        // var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

        logger.LogInformation(
            "[Worker] Processing: Source={Source} | FbMessageId={FbId} | Content={Content}",
            task.Source, task.FbMessageId, task.Content[..Math.Min(50, task.Content.Length)]);

        // Placeholder — Sprint 2 sẽ thay bằng logic thực
        await Task.Delay(100, ct);

        // Sau khi tạo Draft Order thành công → push SignalR
        await hubContext.Clients.All.SendAsync("NewOrderReceived", new
        {
            message = $"[Dev] New event from {task.Source}: {task.Content[..Math.Min(30, task.Content.Length)]}..."
        }, ct);
    }
}
