using FoodOrderBot.Domain.Entities;
using FoodOrderBot.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodOrderBot.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedOnAdd();

        builder.Property(o => o.Status)
            .HasConversion<string>()  // Lưu enum dạng string để dễ đọc trong DB
            .HasMaxLength(20);

        builder.Property(o => o.PaymentStatus)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(o => o.TotalAmount).HasPrecision(18, 2);
        builder.Property(o => o.ReceiverName).HasMaxLength(200);
        builder.Property(o => o.ReceiverPhone).HasMaxLength(20);
        builder.Property(o => o.DeliveryAddress).HasMaxLength(500);
        builder.Property(o => o.PaymentMethod).HasMaxLength(50);

        builder.Property(o => o.TrackingToken)
            .HasMaxLength(64)
            .IsRequired();
        builder.HasIndex(o => o.TrackingToken).IsUnique();  // Bảo đảm unique tracking token

        builder.HasOne(o => o.Shop).WithMany(s => s.Orders).HasForeignKey(o => o.ShopId);
        builder.HasOne(o => o.Customer).WithMany(c => c.Orders).HasForeignKey(o => o.CustomerId);
        builder.HasOne(o => o.RawMessage).WithOne(r => r.Order).HasForeignKey<Order>(o => o.RawMessageId);
        builder.HasMany(o => o.Items).WithOne(i => i.Order).HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);
    }
}
