using Meezan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Meezan.Infrastructure.Persistence.Configurations.Funds;

public class FundCategoryOverrideConfiguration : IEntityTypeConfiguration<FundCategoryOverride>
{
    public void Configure(EntityTypeBuilder<FundCategoryOverride> builder)
    {
        builder.ToTable("FundCategoryOverrides");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.FundId)
            .IsRequired();

        builder.Property(e => e.CategoryId)
            .IsRequired();

        builder.HasIndex(e => new { e.FundId, e.CategoryId })
            .IsUnique();

        builder.Property(e => e.Action)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(100);

        builder.HasOne(e => e.Fund)
            .WithMany(f => f.CategoryOverrides)
            .HasForeignKey(e => e.FundId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Category)
            .WithMany(c => c.Overrides)
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
