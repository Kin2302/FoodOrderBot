using FoodOrderBot.Domain.Entities;
using FoodOrderBot.Domain.Interfaces;
using FoodOrderBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderBot.Infrastructure.Persistence.Repositories;

public class OrderRepository(AppDbContext db) : IOrderRepository
{
    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.MenuItem)
            .Include(o => o.Customer)
            .Include(o => o.RawMessage)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<Order?> GetByTrackingTokenAsync(string token, CancellationToken ct = default)
        => await db.Orders
            .Include(o => o.Items)
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.TrackingToken == token, ct);

    public async Task<IEnumerable<Order>> GetByShopIdAsync(Guid shopId, CancellationToken ct = default)
        => await db.Orders
            .Where(o => o.ShopId == shopId)
            .Include(o => o.Items)
            .Include(o => o.Customer)
            .Include(o => o.RawMessage)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

    public async Task AddAsync(Order order, CancellationToken ct = default)
        => await db.Orders.AddAsync(order, ct);

    public Task UpdateAsync(Order order, CancellationToken ct = default)
    {
        db.Orders.Update(order);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await db.SaveChangesAsync(ct);
}
