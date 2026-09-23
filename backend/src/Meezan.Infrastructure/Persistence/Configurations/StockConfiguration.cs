using Meezan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Meezan.Infrastructure.Persistence.Configurations;

public class StockConfiguration : IEntityTypeConfiguration<Stock>
{
    public void Configure(EntityTypeBuilder<Stock> builder)
    {
        builder.ToTable("Stocks");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Ticker)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(e => e.Ticker)
            .IsUnique();

        builder.Property(e => e.NameAr)
            .HasMaxLength(250);

        builder.Property(e => e.NameEn)
            .HasMaxLength(250);

        builder.Property(e => e.DataStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);
            // NOTE: Do NOT use HasDefaultValue() or HasDefaultValueSql() here.
            // In EF Core, any HasDefaultValue* call on a non-nullable property automatically
            // implies ValueGeneratedOnAdd(), which causes EF to generate a concurrency-check
            // WHERE clause on every UPDATE (WHERE DataStatus = @original), resulting in
            // DbUpdateConcurrencyException (0 rows affected) on every scrape commit.
            // The DB-level DEFAULT 'Active' is set directly in the migration SQL instead.
            // The Stock entity's C# initializer (DataStatus = StockDataStatus.Active) ensures
            // new inserts always send the correct value explicitly.

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        builder.HasOne(e => e.Sector)
            .WithMany(s => s.Stocks)
            .HasForeignKey(e => e.SectorId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
