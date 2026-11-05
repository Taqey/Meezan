using Meezan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Meezan.Infrastructure.Persistence.Configurations.Stocks;

public class SectorDailyConfiguration : IEntityTypeConfiguration<SectorDaily>
{
    public void Configure(EntityTypeBuilder<SectorDaily> builder)
    {
        builder.ToTable("SectorDailies");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.ChangePct).HasColumnType("decimal(18,4)");
        builder.Property(s => s.ChangePctUncapped).HasColumnType("decimal(18,4)");
        builder.Property(s => s.TotalMarketCap).HasColumnType("decimal(24,4)");
        builder.Property(s => s.CoveragePct).HasColumnType("decimal(18,4)");
        builder.Property(s => s.ShareOfWeightRose).HasColumnType("decimal(18,4)");
        builder.Property(s => s.MethodUsed).HasMaxLength(40).IsRequired();
        builder.Property(s => s.TopContributorsJson).HasMaxLength(4000).IsRequired();

        builder.HasIndex(s => new { s.SectorId, s.TradingDate }).IsUnique();

        builder.HasOne(s => s.Sector)
            .WithMany()
            .HasForeignKey(s => s.SectorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
