using FoodOrderBot.Domain.Entities;
using FoodOrderBot.Domain.Interfaces;
using FoodOrderBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderBot.Infrastructure.Persistence.Repositories;

public class MenuItemRepository(AppDbContext db) : IMenuItemRepository
{
    public async Task<IEnumerable<MenuItem>> GetByShopIdAsync(Guid shopId, CancellationToken ct = default)
        => await db.MenuItems
            .Where(m => m.ShopId == shopId)
            .OrderBy(m => m.DisplayOrder)
            .ThenBy(m => m.Name)
            .ToListAsync(ct);

    public async Task<MenuItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.MenuItems.FindAsync([id], ct);

    public async Task AddAsync(MenuItem item, CancellationToken ct = default)
        => await db.MenuItems.AddAsync(item, ct);

    public Task UpdateAsync(MenuItem item, CancellationToken ct = default)
    {
        db.MenuItems.Update(item);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await db.SaveChangesAsync(ct);
}
