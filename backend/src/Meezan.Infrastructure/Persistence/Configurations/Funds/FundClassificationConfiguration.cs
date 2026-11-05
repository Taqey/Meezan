using Meezan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Meezan.Infrastructure.Persistence.Configurations.Funds;

public class FundClassificationNodeConfiguration : IEntityTypeConfiguration<FundClassificationNode>
{
    public void Configure(EntityTypeBuilder<FundClassificationNode> builder)
    {
        builder.ToTable("FundClassificationNodes");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ParentId)
            .IsRequired(false);

        builder.Property(e => e.Level)
            .IsRequired();

        builder.Property(e => e.Slug)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.NameAr)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.NameEn)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Icon)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Color)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.DisplayOrder)
            .IsRequired();

        builder.Property(e => e.IsVisible)
            .IsRequired();

        builder.Property(e => e.IsDeleted)
            .IsRequired();

        builder.Property(e => e.DeletedReason)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.RequiresChildSelection)
            .IsRequired();

        builder.Property(e => e.ChildrenLabelAr)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.ChildrenLabelEn)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.AutoRuleJson)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(e => e.AutoClassificationEnabled)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        builder.Property(e => e.CreatedBy)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.UpdatedBy)
            .IsRequired()
            .HasMaxLength(100);

        // Unique slug across the entire tree (including soft-deleted)
        builder.HasIndex(e => e.Slug)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0"); // Only unique among non-deleted

        // Parent-child relationship
        builder.HasOne(e => e.Parent)
            .WithMany(c => c.Children)
            .HasForeignKey(e => e.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Self-referencing index for tree queries
        builder.HasIndex(e => new { e.ParentId, e.DisplayOrder });

        // Index for tree traversal
        builder.HasIndex(e => e.Level);

        // Soft delete filter
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
