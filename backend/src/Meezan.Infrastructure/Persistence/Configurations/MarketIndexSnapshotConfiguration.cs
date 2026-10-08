using Meezan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Meezan.Infrastructure.Persistence.Configurations;

public class MarketIndexSnapshotConfiguration : IEntityTypeConfiguration<MarketIndexSnapshot>
{
    public void Configure(EntityTypeBuilder<MarketIndexSnapshot> builder)
    {
        builder.ToTable("MarketIndexSnapshots");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Kind).IsRequired().HasMaxLength(20);
        builder.Property(s => s.Code).IsRequired().HasMaxLength(60);
        builder.Property(s => s.MubasherCode).IsRequired().HasMaxLength(60);
        builder.Property(s => s.SourceDateText).HasMaxLength(120);
        builder.Property(s => s.Source).IsRequired().HasMaxLength(40);

        builder.Property(s => s.Value).HasColumnType("decimal(18,4)");
        builder.Property(s => s.Change).HasColumnType("decimal(18,4)");
        builder.Property(s => s.ChangePct).HasColumnType("decimal(18,4)");

        builder.HasIndex(s => new { s.Kind, s.Code }).IsUnique();
    }
}
