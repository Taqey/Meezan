using Meezan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Meezan.Infrastructure.Persistence.Configurations;

public class StockMarketDataConfiguration : IEntityTypeConfiguration<StockMarketData>
{
    public void Configure(EntityTypeBuilder<StockMarketData> builder)
    {
        builder.ToTable("StockMarketData");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Currency).HasMaxLength(20);
        builder.Property(m => m.SourceLastUpdateText).HasMaxLength(200);

        builder.Property(m => m.NominalValue).HasColumnType("decimal(18,4)");
        builder.Property(m => m.MarketValue).HasColumnType("decimal(18,4)");
        builder.Property(m => m.BookValue).HasColumnType("decimal(18,4)");
        builder.Property(m => m.PbRatio).HasColumnType("decimal(18,4)");
        builder.Property(m => m.Eps).HasColumnType("decimal(18,4)");
        builder.Property(m => m.PeRatio).HasColumnType("decimal(18,4)");
        builder.Property(m => m.High).HasColumnType("decimal(18,4)");
        builder.Property(m => m.Low).HasColumnType("decimal(18,4)");
        builder.Property(m => m.Open).HasColumnType("decimal(18,4)");
        builder.Property(m => m.ClosingPrice).HasColumnType("decimal(18,4)");
        builder.Property(m => m.FetchedAt).IsRequired();
        builder.Property(m => m.SlowDataFetchedAt).IsRequired(false);

        // One-to-one with Stock
        builder.HasOne(m => m.Stock)
            .WithOne(s => s.MarketData)
            .HasForeignKey<StockMarketData>(m => m.StockId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => m.StockId).IsUnique();
    }
}
