using Meezan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Meezan.Infrastructure.Persistence.Configurations;

public class SectorConfiguration : IEntityTypeConfiguration<Sector>
{
    public void Configure(EntityTypeBuilder<Sector> builder)
    {
        builder.ToTable("Sectors");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.NameAr)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(e => e.NameEn)
            .IsRequired()
            .HasMaxLength(150);

        builder.HasIndex(e => e.NameEn);
        builder.HasIndex(e => e.NameAr);
    }
}
