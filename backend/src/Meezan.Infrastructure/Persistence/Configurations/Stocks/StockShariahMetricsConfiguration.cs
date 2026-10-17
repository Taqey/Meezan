using Meezan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Meezan.Infrastructure.Persistence.Configurations.Stocks;

public class StockShariahMetricsConfiguration : IEntityTypeConfiguration<StockShariahMetrics>
{
    public void Configure(EntityTypeBuilder<StockShariahMetrics> builder)
    {
        builder.ToTable("StockShariahMetrics");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Zakat).HasPrecision(18, 4);
        builder.Property(e => e.SpHaramEarningPercentage).HasPrecision(18, 4);
        builder.Property(e => e.AaoifiHaramEarningPerShare).HasPrecision(18, 6);
        builder.Property(e => e.HaramEarningsPercentage).HasPrecision(18, 4);
        builder.Property(e => e.LoansPercentage).HasPrecision(18, 4);
        builder.Property(e => e.FairValueValuation).HasPrecision(18, 4);
        builder.Property(e => e.BookValue).HasPrecision(18, 4);
        builder.Property(e => e.Profit).HasPrecision(18, 4);
        builder.Property(e => e.Dividend).HasPrecision(18, 4);

        builder.Property(e => e.DividendType).HasMaxLength(100);
        builder.Property(e => e.CategoryEn).HasMaxLength(250);
        builder.Property(e => e.CategoryAr).HasMaxLength(250);

        // Override columns
        builder.Property(e => e.CoreActivityCompliantOverride);
        builder.Property(e => e.CategoryArOverride).HasMaxLength(250);
        builder.Property(e => e.CategoryEnOverride).HasMaxLength(250);
        builder.Property(e => e.HaramRevenuePercentageOverride).HasPrecision(18, 4);
        builder.Property(e => e.LoansPercentageOverride).HasPrecision(18, 4);

        builder.Property(e => e.FetchedAt).IsRequired();

        builder.HasIndex(e => e.StockId)
            .IsUnique();

        builder.HasOne(e => e.Stock)
            .WithOne(s => s.ShariahMetrics)
            .HasForeignKey<StockShariahMetrics>(e => e.StockId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
