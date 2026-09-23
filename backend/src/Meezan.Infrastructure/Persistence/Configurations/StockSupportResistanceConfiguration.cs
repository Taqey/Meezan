using Meezan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Meezan.Infrastructure.Persistence.Configurations;

public class StockSupportResistanceConfiguration : IEntityTypeConfiguration<StockSupportResistance>
{
    public void Configure(EntityTypeBuilder<StockSupportResistance> builder)
    {
        builder.ToTable("StockSupportResistance");
        builder.HasKey(sr => sr.Id);

        builder.Property(sr => sr.LastPrice).HasColumnType("decimal(18,4)");
        builder.Property(sr => sr.ChangePct).HasColumnType("decimal(18,4)");
        builder.Property(sr => sr.Pivot).HasColumnType("decimal(18,4)");
        builder.Property(sr => sr.R1).HasColumnType("decimal(18,4)");
        builder.Property(sr => sr.R2).HasColumnType("decimal(18,4)");
        builder.Property(sr => sr.S1).HasColumnType("decimal(18,4)");
        builder.Property(sr => sr.S2).HasColumnType("decimal(18,4)");

        // One-to-one with Stock
        builder.HasOne(sr => sr.Stock)
            .WithOne(s => s.SupportResistance)
            .HasForeignKey<StockSupportResistance>(sr => sr.StockId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(sr => sr.StockId).IsUnique();
    }
}
