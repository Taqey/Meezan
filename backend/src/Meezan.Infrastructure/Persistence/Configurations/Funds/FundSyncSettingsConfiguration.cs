using Meezan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Meezan.Infrastructure.Persistence.Configurations.Funds;

public class FundSyncSettingsConfiguration : IEntityTypeConfiguration<FundSyncSettings>
{
    public void Configure(EntityTypeBuilder<FundSyncSettings> builder)
    {
        builder.ToTable("FundSyncSettings");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.IsEnabled)
            .IsRequired();

        builder.Property(e => e.RunTimeCairo)
            .IsRequired();

        builder.Property(e => e.FullSyncScheduleEnabled)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.FullSyncRunTimeCairo)
            .IsRequired()
            .HasDefaultValue(new TimeOnly(2, 0));

        builder.Property(e => e.PreventOverlap)
            .IsRequired();

        builder.Property(e => e.RequestDelaySeconds)
            .IsRequired();

        builder.Property(e => e.MaxRetries)
            .IsRequired();

        builder.Property(e => e.RetryBackoffSeconds)
            .IsRequired();

        builder.Property(e => e.AutoIndexFundTypeEnabled)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.AutoClassificationEnabled)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedBy)
            .HasMaxLength(100);
    }
}
