using FoodOrderBot.Domain.Entities;
using FoodOrderBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodOrderBot.Infrastructure.Persistence;

/// <summary>
/// Khởi tạo database và seed data mẫu khi startup (chỉ chạy nếu chưa có data).
/// </summary>
public class DbInitializer(AppDbContext db, ILogger<DbInitializer> logger)
{
    // ID cố định để dễ reference trong config và test
    public static readonly Guid DefaultShopId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public async Task InitialiseAsync(CancellationToken ct = default)
    {
        try
        {
            // Áp dụng migration còn pending (tự tạo DB nếu chưa có)
            await db.Database.MigrateAsync(ct);
            logger.LogInformation("Database migration completed.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Migration failed.");
            throw;
        }
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        // Không seed lại nếu đã có shop
        if (await db.Shops.AnyAsync(ct))
        {
            logger.LogInformation("Seed data already exists, skipping.");
            return;
        }

        logger.LogInformation("Seeding initial data...");

        var shop = new Shop
        {
            Id = DefaultShopId,
            Name = "Cơm Tấm Bà Ba",
            Phone = "0909123456",
            FbPageId = "demo_page_id",
            FbAccessToken = "demo_access_token",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var menuItems = new List<MenuItem>
        {
            new() { Id = Guid.NewGuid(), ShopId = DefaultShopId, Name = "Cơm tấm sườn bì chả", Price = 45000, IsAvailable = true },
            new() { Id = Guid.NewGuid(), ShopId = DefaultShopId, Name = "Cơm tấm sườn nướng", Price = 40000, IsAvailable = true },
            new() { Id = Guid.NewGuid(), ShopId = DefaultShopId, Name = "Cơm tấm bì chả", Price = 35000, IsAvailable = true },
            new() { Id = Guid.NewGuid(), ShopId = DefaultShopId, Name = "Bún bò Huế", Price = 45000, IsAvailable = true },
            new() { Id = Guid.NewGuid(), ShopId = DefaultShopId, Name = "Phở bò tái nạm", Price = 50000, IsAvailable = true },
            new() { Id = Guid.NewGuid(), ShopId = DefaultShopId, Name = "Bánh mì thịt nguội", Price = 25000, IsAvailable = true },
            new() { Id = Guid.NewGuid(), ShopId = DefaultShopId, Name = "Hủ tiếu Nam Vang", Price = 45000, IsAvailable = true },
            new() { Id = Guid.NewGuid(), ShopId = DefaultShopId, Name = "Cháo lòng", Price = 35000, IsAvailable = true },
            new() { Id = Guid.NewGuid(), ShopId = DefaultShopId, Name = "Nước chanh", Price = 15000, IsAvailable = true },
            new() { Id = Guid.NewGuid(), ShopId = DefaultShopId, Name = "Trà đá", Price = 5000, IsAvailable = true },
        };

        await db.Shops.AddAsync(shop, ct);
        await db.MenuItems.AddRangeAsync(menuItems, ct);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Seeded 1 shop and {Count} menu items.", menuItems.Count);
    }
}
