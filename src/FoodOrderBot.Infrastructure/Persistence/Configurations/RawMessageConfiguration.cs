using FoodOrderBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodOrderBot.Infrastructure.Persistence.Configurations;

public class RawMessageConfiguration : IEntityTypeConfiguration<RawMessage>
{
    public void Configure(EntityTypeBuilder<RawMessage> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedOnAdd();

        builder.Property(r => r.Source)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(r => r.FbMessageId).HasMaxLength(200).IsRequired();
        builder.HasIndex(r => r.FbMessageId).IsUnique();  // Deduplication index

        builder.Property(r => r.Content).HasColumnType("text");
        builder.Property(r => r.ParsedResult).HasColumnType("jsonb");  // PostgreSQL JSONB

        builder.HasOne(r => r.Shop).WithMany(s => s.RawMessages).HasForeignKey(r => r.ShopId);
    }
}
