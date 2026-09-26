using Meezan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Meezan.Infrastructure.Persistence.Configurations;

public class IndexConstituentConfiguration : IEntityTypeConfiguration<IndexConstituent>
{
    public void Configure(EntityTypeBuilder<IndexConstituent> builder)
    {
        builder.ToTable("IndexConstituents");

        builder.HasKey(e => e.Id);

        // Nullable → NULL allowed for weight-less source files.
        builder.Property(e => e.Weight)
            .HasPrecision(18, 8);

        builder.Property(e => e.EffectiveDate)
            .IsRequired();

        builder.HasOne(e => e.Index)
            .WithMany(i => i.Constituents)
            .HasForeignKey(e => e.IndexId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Stock)
            .WithMany(s => s.IndexConstituents)
            .HasForeignKey(e => e.StockId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.IndexId, e.StockId });
    }
}
