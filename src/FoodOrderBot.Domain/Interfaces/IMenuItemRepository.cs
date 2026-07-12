using FoodOrderBot.Domain.Entities;

namespace FoodOrderBot.Domain.Interfaces;

public interface IMenuItemRepository
{
    Task<IEnumerable<MenuItem>> GetByShopIdAsync(Guid shopId, CancellationToken ct = default);
    Task<MenuItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(MenuItem item, CancellationToken ct = default);
    Task UpdateAsync(MenuItem item, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
