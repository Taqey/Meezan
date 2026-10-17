using Meezan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Meezan.Infrastructure.Persistence.Configurations.Funds;

public class FundSyncLogConfiguration : IEntityTypeConfiguration<FundSyncLog>
{
    public void Configure(EntityTypeBuilder<FundSyncLog> builder)
    {
        builder.ToTable("FundSyncLogs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.RunAt)
            .IsRequired();

        builder.Property(e => e.FinishedAt);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.TotalFunds)
            .IsRequired();

        builder.Property(e => e.UpdatedFunds)
            .IsRequired();

        builder.Property(e => e.FailedFunds)
            .IsRequired();

        builder.Property(e => e.SkippedFunds)
            .IsRequired();

        builder.Property(e => e.ErrorSummary)
            .HasMaxLength(1000);

        builder.Property(e => e.TriggeredBy)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.DurationSeconds)
            .IsRequired();

        builder.HasMany(e => e.Details)
            .WithOne(d => d.FundSyncLog)
            .HasForeignKey(d => d.FundSyncLogId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class FundSyncLogDetailConfiguration : IEntityTypeConfiguration<FundSyncLogDetail>
{
    public void Configure(EntityTypeBuilder<FundSyncLogDetail> builder)
    {
        builder.ToTable("FundSyncLogDetails");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.FundSyncLogId)
            .IsRequired();

        builder.Property(e => e.FundId)
            .IsRequired();

        builder.Property(e => e.FundCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(1000);

        builder.Property(e => e.AttemptedAt)
            .IsRequired();

        builder.HasOne(e => e.FundSyncLog)
            .WithMany(l => l.Details)
            .HasForeignKey(e => e.FundSyncLogId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

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
