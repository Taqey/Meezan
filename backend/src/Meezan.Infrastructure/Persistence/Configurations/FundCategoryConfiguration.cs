using Meezan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Meezan.Infrastructure.Persistence.Configurations;

public class FundCategoryConfiguration : IEntityTypeConfiguration<FundCategory>
{
    public void Configure(EntityTypeBuilder<FundCategory> builder)
    {
        builder.ToTable("FundCategories");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.NameAr)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.NameEn)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Slug)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(e => e.Slug)
            .IsUnique();

        builder.Property(e => e.Icon)
            .HasMaxLength(50);

        builder.Property(e => e.Color)
            .HasMaxLength(20);

        builder.Property(e => e.IsAutoDerived)
            .IsRequired();

        builder.Property(e => e.IsIndexTracking)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.AutoRuleJson)
            .HasMaxLength(2000);

        builder.Property(e => e.IsVisible)
            .IsRequired();

        builder.Property(e => e.DisplayOrder)
            .IsRequired();

        builder.Property(e => e.IsDeleted)
            .IsRequired();

        builder.Property(e => e.DeletedAt);

        builder.Property(e => e.DeletedReason)
            .HasMaxLength(500);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(100);

        builder.Property(e => e.UpdatedBy)
            .HasMaxLength(100);

        builder.HasMany(e => e.Overrides)
            .WithOne(o => o.Category)
            .HasForeignKey(o => o.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

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