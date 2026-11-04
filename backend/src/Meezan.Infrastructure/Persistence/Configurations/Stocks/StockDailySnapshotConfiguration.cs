using Meezan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Meezan.Infrastructure.Persistence.Configurations.Stocks;

public class StockDailySnapshotConfiguration : IEntityTypeConfiguration<StockDailySnapshot>
{
    public void Configure(EntityTypeBuilder<StockDailySnapshot> builder)
    {
        builder.ToTable("StockDailySnapshots");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Ticker).HasMaxLength(30).IsRequired();
        builder.Property(s => s.Price).HasColumnType("decimal(18,4)");
        builder.Property(s => s.PrevClose).HasColumnType("decimal(18,4)");
        builder.Property(s => s.ChangePct).HasColumnType("decimal(18,4)");
        builder.Property(s => s.SharesUsed).HasColumnType("decimal(24,4)");
        builder.Property(s => s.Currency).HasMaxLength(30);

        builder.HasIndex(s => new { s.StockId, s.TradingDate }).IsUnique();
        builder.HasIndex(s => s.TradingDate);

        builder.HasOne(s => s.Stock)
            .WithMany()
            .HasForeignKey(s => s.StockId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Sector)
            .WithMany()
            .HasForeignKey(s => s.SectorId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
