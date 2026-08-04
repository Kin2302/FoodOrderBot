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

    /// <summary>
    /// Lấy danh sách complaints (Intent = "Complaint", Role = "User") của shop,
    /// sắp xếp theo thời gian mới nhất.
    /// </summary>
    Task<List<ConversationMessage>> GetComplaintsByShopAsync(
        Guid shopId,
        int limit = 50,
        CancellationToken ct = default);

    /// <summary>
    /// Lấy toàn bộ lịch sử hội thoại của 1 khách theo fbSenderId.
    /// </summary>
    Task<List<ConversationMessage>> GetAllBySenderAsync(
        string fbSenderId,
        Guid shopId,
        CancellationToken ct = default);

    /// <summary>
    /// Lấy tất cả ConversationMessages của shop trong khoảng thời gian (cho analytics).
    /// </summary>
    Task<List<ConversationMessage>> GetByShopIdAndDateRangeAsync(
        Guid shopId,
        DateTime from,
        DateTime to,
        CancellationToken ct = default);

    Task AddAsync(ConversationMessage message, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
