using Meezan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Meezan.Infrastructure.Persistence.Configurations.Funds;

public class FundPriceHistoryConfiguration : IEntityTypeConfiguration<FundPriceHistory>
{
    public void Configure(EntityTypeBuilder<FundPriceHistory> builder)
    {
        builder.ToTable("FundPriceHistory");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.FundId)
            .IsRequired();

        builder.HasIndex(e => new { e.FundId, e.Date })
            .IsUnique();

        builder.Property(e => e.Date)
            .IsRequired();

        builder.Property(e => e.Price)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(e => e.Change)
            .HasPrecision(10, 4);

        builder.Property(e => e.Performance)
            .HasPrecision(10, 4);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.HasOne(e => e.Fund)
            .WithMany(f => f.PriceHistory)
            .HasForeignKey(e => e.FundId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
