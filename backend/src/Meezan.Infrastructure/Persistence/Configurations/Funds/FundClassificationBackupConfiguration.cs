using Meezan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Meezan.Infrastructure.Persistence.Configurations.Funds;

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
