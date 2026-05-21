using FoodOrderBot.Domain.Entities;

namespace FoodOrderBot.Domain.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Order?> GetByTrackingTokenAsync(string token, CancellationToken ct = default);
    Task<IEnumerable<Order>> GetByShopIdAsync(Guid shopId, CancellationToken ct = default);
    Task AddAsync(Order order, CancellationToken ct = default);
    Task UpdateAsync(Order order, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
