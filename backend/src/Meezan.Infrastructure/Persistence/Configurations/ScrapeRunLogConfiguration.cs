using Meezan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Meezan.Infrastructure.Persistence.Configurations;

public class ScrapeRunLogConfiguration : IEntityTypeConfiguration<ScrapeRunLog>
{
    public void Configure(EntityTypeBuilder<ScrapeRunLog> builder)
    {
        builder.ToTable("ScrapeRunLogs");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.TriggeredBy).IsRequired().HasMaxLength(50);
        builder.Property(l => l.ErrorSummary).HasMaxLength(4000);
    }
}
