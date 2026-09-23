using Meezan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Meezan.Infrastructure.Persistence.Configurations;

public class UploadHistoryConfiguration : IEntityTypeConfiguration<UploadHistory>
{
    public void Configure(EntityTypeBuilder<UploadHistory> builder)
    {
        builder.ToTable("UploadHistories");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.FileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(2000);

        builder.Property(e => e.UploadedBy)
            .HasMaxLength(100);

        builder.Property(e => e.UploadedAt)
            .IsRequired();

        builder.HasOne(e => e.Index)
            .WithMany(i => i.UploadHistories)
            .HasForeignKey(e => e.IndexId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
