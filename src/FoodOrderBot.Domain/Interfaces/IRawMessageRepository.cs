using FoodOrderBot.Domain.Entities;

namespace FoodOrderBot.Domain.Interfaces;

public interface IRawMessageRepository
{
    /// <summary>
    /// Kiểm tra xem FbMessageId đã tồn tại chưa — dùng để dedup webhook
    /// </summary>
    Task<bool> ExistsByFbMessageIdAsync(string fbMessageId, CancellationToken ct = default);
    Task AddAsync(RawMessage rawMessage, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
