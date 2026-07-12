using FoodOrderBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodOrderBot.Infrastructure.Persistence.Configurations;

public class ConversationMessageConfiguration : IEntityTypeConfiguration<ConversationMessage>
{
    public void Configure(EntityTypeBuilder<ConversationMessage> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedOnAdd();

        builder.Property(c => c.FbSenderId).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Role).HasMaxLength(20).IsRequired();
        builder.Property(c => c.Content).HasColumnType("text").IsRequired();
        builder.Property(c => c.Intent).HasMaxLength(50);

        // Index để query nhanh 5 tin gần nhất theo sender
        builder.HasIndex(c => new { c.FbSenderId, c.CreatedAt })
            .HasDatabaseName("IX_ConversationMessages_FbSenderId_CreatedAt");

        builder.HasOne(c => c.Shop)
            .WithMany()
            .HasForeignKey(c => c.ShopId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
