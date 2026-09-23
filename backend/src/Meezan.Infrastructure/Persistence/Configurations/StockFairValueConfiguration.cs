using Meezan.Domain.Entities;
using Meezan.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Meezan.Infrastructure.Persistence.Configurations;

public class StockFairValueConfiguration : IEntityTypeConfiguration<StockFairValue>
{
    public void Configure(EntityTypeBuilder<StockFairValue> builder)
    {
        builder.ToTable("StockFairValues");
        builder.HasKey(fv => fv.Id);

        builder.Property(fv => fv.FairValue).HasColumnType("decimal(18,4)");
        builder.Property(fv => fv.FairValueDiff).HasColumnType("decimal(18,4)");
        builder.Property(fv => fv.FairValueDiffPct).HasColumnType("decimal(18,4)");

        builder.Property(fv => fv.PriceComparison)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(fv => fv.Confidence)
            .HasConversion<string>()
            .HasMaxLength(20);

        // One-to-one with Stock
        builder.HasOne(fv => fv.Stock)
            .WithOne(s => s.FairValue)
            .HasForeignKey<StockFairValue>(fv => fv.StockId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(fv => fv.StockId).IsUnique();
    }
}

public class StockFairValueMethodConfiguration : IEntityTypeConfiguration<StockFairValueMethod>
{
    public void Configure(EntityTypeBuilder<StockFairValueMethod> builder)
    {
        builder.ToTable("StockFairValueMethods");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.MethodName).IsRequired().HasMaxLength(50);
        builder.Property(m => m.EstimatedValue).HasColumnType("decimal(18,4)");

        builder.HasOne(m => m.StockFairValue)
            .WithMany(fv => fv.Methods)
            .HasForeignKey(m => m.StockFairValueId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
