using FoodOrderBot.Domain.Entities;
using FoodOrderBot.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;


namespace FoodOrderBot.Infrastructure.Persistence.Repositories;

    public class ShopRepository(AppDbContext db) : IShopRepository
    {
        public async Task AddAsync(Shop shop, CancellationToken ct = default)
        {
            await db.Shops.AddAsync(shop, ct);
        }

        public async Task<Shop?> GetByFbPageIdAsync(string fbPageId, CancellationToken ct = default)
        {
            return await db.Shops.FirstOrDefaultAsync(s => s.FbPageId == fbPageId, ct);
        }

        public async Task SaveChangesAsync(CancellationToken ct = default)
        {
            await db.SaveChangesAsync(ct);
        }

        public Task UpdateAsync(Shop shop, CancellationToken ct = default)
        {
            db.Shops.Update(shop);
            return Task.CompletedTask;
        }
    }
