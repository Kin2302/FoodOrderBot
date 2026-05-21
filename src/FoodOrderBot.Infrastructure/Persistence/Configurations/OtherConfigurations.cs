using FoodOrderBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodOrderBot.Infrastructure.Persistence.Configurations;

public class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedOnAdd();
        builder.Property(m => m.Name).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Price).HasPrecision(18, 2);
        builder.Property(m => m.Description).HasColumnType("text");
        builder.HasOne(m => m.Shop).WithMany(s => s.MenuItems).HasForeignKey(m => m.ShopId);
    }
}

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedOnAdd();
        builder.Property(c => c.FbSenderId).HasMaxLength(100).IsRequired();
        builder.HasIndex(c => c.FbSenderId).IsUnique();  // 1 PSID = 1 customer
        builder.Property(c => c.Name).HasMaxLength(200);
        builder.Property(c => c.Phone).HasMaxLength(20);
    }
}

public class ShopConfiguration : IEntityTypeConfiguration<Shop>
{
    public void Configure(EntityTypeBuilder<Shop> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedOnAdd();
        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.FbPageId).HasMaxLength(100);
        builder.Property(s => s.FbAccessToken).HasColumnType("text");  // Token dài
    }
}

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedOnAdd();
        builder.Property(i => i.ItemName).HasMaxLength(200);
        builder.Property(i => i.UnitPrice).HasPrecision(18, 2);
        builder.Property(i => i.Note).HasMaxLength(500);
        builder.HasOne(i => i.MenuItem).WithMany(m => m.OrderItems).HasForeignKey(i => i.MenuItemId);
    }
}
