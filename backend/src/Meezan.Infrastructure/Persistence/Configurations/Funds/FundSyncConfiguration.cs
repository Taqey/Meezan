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

        builder.Property(e => e.Mode)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("Full");

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
