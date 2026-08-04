using FoodOrderBot.Domain.Entities;

namespace FoodOrderBot.Domain.Interfaces
{
    public interface IShopRepository
    {
        Task<Shop?> GetByFbPageIdAsync(string fbPageId, CancellationToken ct = default);
        Task AddAsync(Shop shop, CancellationToken ct = default);
        Task UpdateAsync(Shop shop, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}
