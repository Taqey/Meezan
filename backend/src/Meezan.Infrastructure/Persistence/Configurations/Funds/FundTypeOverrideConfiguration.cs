using Meezan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Meezan.Infrastructure.Persistence.Configurations.Funds;

public class FundTypeOverrideConfiguration : IEntityTypeConfiguration<FundTypeOverride>
{
    public void Configure(EntityTypeBuilder<FundTypeOverride> builder)
    {
        builder.ToTable("FundTypeOverrides");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);

        builder.HasIndex(e => e.FundId).IsUnique();

        builder.HasOne(e => e.Fund)
            .WithMany()
            .HasForeignKey(e => e.FundId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Definition)
            .WithMany(d => d.Overrides)
            .HasForeignKey(e => e.FundTypeDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
