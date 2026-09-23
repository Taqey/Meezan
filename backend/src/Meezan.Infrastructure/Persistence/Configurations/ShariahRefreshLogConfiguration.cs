using Meezan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Meezan.Infrastructure.Persistence.Configurations;

public class ShariahRefreshLogConfiguration : IEntityTypeConfiguration<ShariahRefreshLog>
{
    public void Configure(EntityTypeBuilder<ShariahRefreshLog> builder)
    {
        builder.ToTable("ShariahRefreshLogs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.RunAt).IsRequired();

        builder.Property(e => e.TriggeredBy)
            .IsRequired()
            .HasMaxLength(50);
    }
}
