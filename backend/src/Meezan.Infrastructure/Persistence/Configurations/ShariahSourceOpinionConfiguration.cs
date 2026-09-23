using Meezan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Meezan.Infrastructure.Persistence.Configurations;

public class ShariahSourceOpinionConfiguration : IEntityTypeConfiguration<ShariahSourceOpinion>
{
    public void Configure(EntityTypeBuilder<ShariahSourceOpinion> builder)
    {
        builder.ToTable("ShariahSourceOpinions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.SourceKey)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.Status)
            .HasMaxLength(100);

        builder.Property(e => e.Percentage)
            .HasPrecision(18, 4);

        builder.Property(e => e.Note)
            .HasMaxLength(2000);

        builder.Property(e => e.PdfUrl)
            .HasMaxLength(1000);

        builder.Property(e => e.ExtraData)
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.FetchedAt)
            .IsRequired();

        builder.HasIndex(e => new { e.StockId, e.SourceKey })
            .IsUnique();

        builder.HasOne(e => e.Stock)
            .WithMany(s => s.ShariahSourceOpinions)
            .HasForeignKey(e => e.StockId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
