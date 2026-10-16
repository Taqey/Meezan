using Meezan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Meezan.Infrastructure.Persistence.Configurations;

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

public class FundClassificationOverrideConfiguration : IEntityTypeConfiguration<FundClassificationOverride>
{
    public void Configure(EntityTypeBuilder<FundClassificationOverride> builder)
    {
        builder.ToTable("FundClassificationOverrides");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.FundId)
            .IsRequired();

        builder.Property(e => e.NodeId)
            .IsRequired();

        builder.Property(e => e.Action)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.CreatedBy)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasOne(e => e.Fund)
            .WithMany()
            .HasForeignKey(e => e.FundId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Node)
            .WithMany(n => n.Overrides)
            .HasForeignKey(e => e.NodeId)
            .OnDelete(DeleteBehavior.Cascade);

        // One override per fund per node
        builder.HasIndex(e => new { e.FundId, e.NodeId })
            .IsUnique();
    }
}

public class FundClassificationBackupConfiguration : IEntityTypeConfiguration<FundClassificationBackup>
{
    public void Configure(EntityTypeBuilder<FundClassificationBackup> builder)
    {
        builder.ToTable("FundClassificationBackups");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.FundId).IsRequired();
        builder.Property(e => e.FundCode).IsRequired().HasMaxLength(50);
        builder.Property(e => e.FundNameAr).HasMaxLength(250);
        builder.Property(e => e.FundNameEn).HasMaxLength(250);
        builder.Property(e => e.OldType).IsRequired();
        builder.Property(e => e.OldTypeSlug).IsRequired().HasMaxLength(100);
        builder.Property(e => e.OldTypeOverrideSlug).HasMaxLength(100);
        builder.Property(e => e.OldCategorySlugs).IsRequired().HasMaxLength(2000);
        builder.Property(e => e.OldManualIncludeSlugs).IsRequired().HasMaxLength(2000);
        builder.Property(e => e.OldManualExcludeSlugs).IsRequired().HasMaxLength(2000);
        builder.Property(e => e.NewPath).HasMaxLength(1000);
        builder.Property(e => e.MigrationReason).HasMaxLength(1000);
        builder.Property(e => e.BackedUpAt).IsRequired();
        builder.Property(e => e.BackedUpBy).IsRequired().HasMaxLength(100);

        builder.HasIndex(e => e.FundId).IsUnique();
        builder.HasIndex(e => e.FundCode);
    }
}

public class FundClassificationChangeLogConfiguration : IEntityTypeConfiguration<FundClassificationChangeLog>
{
    public void Configure(EntityTypeBuilder<FundClassificationChangeLog> builder)
    {
        builder.ToTable("FundClassificationChangeLogs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.FundId)
            .IsRequired(false);

        builder.Property(e => e.NodeId)
            .IsRequired(false);

        builder.Property(e => e.Action)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.PerformedBy)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Reason)
            .HasMaxLength(500);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.HasOne(e => e.Fund)
            .WithMany()
            .HasForeignKey(e => e.FundId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.Node)
            .WithMany(n => n.ChangeLogs)
            .HasForeignKey(e => e.NodeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.NodeId);
        builder.HasIndex(e => e.FundId);
        builder.HasIndex(e => e.CreatedAt);
    }
}