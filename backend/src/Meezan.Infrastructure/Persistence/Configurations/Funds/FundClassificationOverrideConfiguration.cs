using Meezan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Meezan.Infrastructure.Persistence.Configurations.Funds;

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
