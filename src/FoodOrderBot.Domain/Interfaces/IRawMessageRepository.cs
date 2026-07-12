using FoodOrderBot.Domain.Entities;

namespace FoodOrderBot.Domain.Interfaces;

public interface IRawMessageRepository
{
    /// <summary>
    /// Kiểm tra xem FbMessageId đã tồn tại chưa — dùng để dedup webhook
    /// </summary>
    Task<bool> ExistsByFbMessageIdAsync(string fbMessageId, CancellationToken ct = default);
    Task AddAsync(RawMessage rawMessage, CancellationToken ct = default);

    /// <summary>
    /// Cập nhật RawMessage (lưu ParsedResult, ParseConfidence sau khi AI xử lý xong)
    /// </summary>
    Task UpdateAsync(RawMessage rawMessage, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
