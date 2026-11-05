using Meezan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Meezan.Infrastructure.Persistence.Configurations.Funds;

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
