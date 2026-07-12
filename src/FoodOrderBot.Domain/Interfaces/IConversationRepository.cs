using FoodOrderBot.Domain.Entities;

namespace FoodOrderBot.Domain.Interfaces;

public interface IConversationRepository
{
    /// <summary>
    /// Lấy N tin nhắn gần nhất của khách theo fbSenderId + shopId,
    /// sắp xếp theo thời gian tăng dần (cũ trước, mới sau) để đưa vào ChatHistory.
    /// </summary>
    Task<List<ConversationMessage>> GetRecentBySenderAsync(
        string fbSenderId,
        Guid shopId,
        int limit = 5,
        CancellationToken ct = default);

    Task AddAsync(ConversationMessage message, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
