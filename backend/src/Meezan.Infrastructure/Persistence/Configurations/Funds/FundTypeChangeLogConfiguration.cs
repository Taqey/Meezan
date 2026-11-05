using Meezan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Meezan.Infrastructure.Persistence.Configurations.Funds;

public class FundTypeChangeLogConfiguration : IEntityTypeConfiguration<FundTypeChangeLog>
{
    public void Configure(EntityTypeBuilder<FundTypeChangeLog> builder)
    {
        builder.ToTable("FundTypeChangeLogs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Action).IsRequired().HasConversion<int>();
        builder.Property(e => e.PerformedBy).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Reason).HasMaxLength(500);
        builder.Property(e => e.CreatedAt).IsRequired();

        builder.HasOne(e => e.Fund)
            .WithMany()
            .HasForeignKey(e => e.FundId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.Definition)
            .WithMany()
            .HasForeignKey(e => e.FundTypeDefinitionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => e.FundTypeDefinitionId);
        builder.HasIndex(e => e.FundId);
        builder.HasIndex(e => e.CreatedAt);
    }
}
