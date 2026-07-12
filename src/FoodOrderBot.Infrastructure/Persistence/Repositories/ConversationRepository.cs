using FoodOrderBot.Domain.Entities;
using FoodOrderBot.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderBot.Infrastructure.Persistence.Repositories;

public class ConversationRepository(AppDbContext db) : IConversationRepository
{
    /// <summary>
    /// Lấy N tin gần nhất theo sender, sắp xếp cũ → mới (để đưa vào ChatHistory đúng thứ tự).
    /// </summary>
    public async Task<List<ConversationMessage>> GetRecentBySenderAsync(
        string fbSenderId,
        Guid shopId,
        int limit = 5,
        CancellationToken ct = default)
    {
        return await db.ConversationMessages
            .Where(c => c.FbSenderId == fbSenderId && c.ShopId == shopId)
            .OrderByDescending(c => c.CreatedAt)
            .Take(limit)
            .OrderBy(c => c.CreatedAt)          // Đảo lại: cũ → mới
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task AddAsync(ConversationMessage message, CancellationToken ct = default)
        => await db.ConversationMessages.AddAsync(message, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
