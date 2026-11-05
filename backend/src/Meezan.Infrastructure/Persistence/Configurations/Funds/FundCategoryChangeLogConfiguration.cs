using Meezan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Meezan.Infrastructure.Persistence.Configurations.Funds;

public class FundCategoryChangeLogConfiguration : IEntityTypeConfiguration<FundCategoryChangeLog>
{
    public void Configure(EntityTypeBuilder<FundCategoryChangeLog> builder)
    {
        builder.ToTable("FundCategoryChangeLogs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.FundId)
            .IsRequired(false);

        builder.Property(e => e.CategoryId)
            .IsRequired();

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

        builder.HasOne(e => e.Category)
            .WithMany()
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
