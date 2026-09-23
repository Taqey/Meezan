using Meezan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Meezan.Infrastructure.Persistence.Configurations;

public class ShariahComplianceConfiguration : IEntityTypeConfiguration<ShariahCompliance>
{
    public void Configure(EntityTypeBuilder<ShariahCompliance> builder)
    {
        builder.ToTable("ShariahCompliances");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.Pct)
            .HasPrecision(18, 4);

        builder.Property(e => e.Note)
            .HasMaxLength(1000);

        builder.Property(e => e.LastCheckedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        builder.HasIndex(e => e.StockId)
            .IsUnique();

        builder.HasOne(e => e.Stock)
            .WithOne(s => s.ShariahCompliance)
            .HasForeignKey<ShariahCompliance>(e => e.StockId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
