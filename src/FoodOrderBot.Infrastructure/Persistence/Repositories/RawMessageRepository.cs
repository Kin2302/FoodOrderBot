using FoodOrderBot.Domain.Entities;
using FoodOrderBot.Domain.Interfaces;
using FoodOrderBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderBot.Infrastructure.Persistence.Repositories;

public class RawMessageRepository(AppDbContext db) : IRawMessageRepository
{
    public async Task<bool> ExistsByFbMessageIdAsync(string fbMessageId, CancellationToken ct = default)
        => await db.RawMessages.AnyAsync(r => r.FbMessageId == fbMessageId, ct);

    public async Task AddAsync(RawMessage rawMessage, CancellationToken ct = default)
        => await db.RawMessages.AddAsync(rawMessage, ct);

    public Task UpdateAsync(RawMessage rawMessage, CancellationToken ct = default)
    {
        db.RawMessages.Update(rawMessage);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await db.SaveChangesAsync(ct);
}
