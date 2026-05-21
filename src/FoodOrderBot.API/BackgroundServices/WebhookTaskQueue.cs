using System.Threading.Channels;

namespace FoodOrderBot.API.BackgroundServices;

/// <summary>
/// Wrapper quanh Channel&lt;T&gt; — hàng đợi in-memory cho webhook events.
/// WebhookController enqueue, WebhookProcessingWorker dequeue và xử lý ngầm.
/// </summary>
public class WebhookTaskQueue
{
    private readonly Channel<WebhookTask> _channel = Channel.CreateUnbounded<WebhookTask>(
        new UnboundedChannelOptions { SingleReader = true });

    public ValueTask EnqueueAsync(WebhookTask task, CancellationToken ct = default)
        => _channel.Writer.WriteAsync(task, ct);

    public IAsyncEnumerable<WebhookTask> ReadAllAsync(CancellationToken ct)
        => _channel.Reader.ReadAllAsync(ct);
}

public record WebhookTask(
    string FbSenderId,
    string FbMessageId,
    string Content,
    string Source,       // "messenger" | "comment"
    string? FbPostId,
    string? FbCommentId,
    Guid ShopId
);
