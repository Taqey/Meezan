using Meezan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Meezan.Infrastructure.Persistence.Configurations.Funds;

public class FundConfiguration : IEntityTypeConfiguration<Fund>
{
    public void Configure(EntityTypeBuilder<Fund> builder)
    {
        builder.ToTable("Funds");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.SndukId)
            .IsRequired();

        builder.HasIndex(e => e.SndukId)
            .IsUnique();

        builder.Property(e => e.PublicId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Slug)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.NameAr)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(e => e.NameEn)
            .HasMaxLength(250);

        builder.Property(e => e.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(e => e.Code)
            .IsUnique();

        builder.Property(e => e.Type)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.TypeNameAr)
            .HasMaxLength(100);

        builder.Property(e => e.TypeNameEn)
            .HasMaxLength(100);

        builder.Property(e => e.RiskLevel)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.IsIslamic)
            .IsRequired();

        builder.Property(e => e.IsFeatured)
            .IsRequired();

        builder.Property(e => e.IsActive)
            .IsRequired();

        builder.Property(e => e.CurrentPrice)
            .HasPrecision(18, 4);

        builder.Property(e => e.DailyChange)
            .HasPrecision(10, 4);

        builder.Property(e => e.Return1M)
            .HasPrecision(10, 4);

        builder.Property(e => e.Return3M)
            .HasPrecision(10, 4);

        builder.Property(e => e.Return6M)
            .HasPrecision(10, 4);

        builder.Property(e => e.Return1Y)
            .HasPrecision(10, 4);

        builder.Property(e => e.Return3Y)
            .HasPrecision(10, 4);

        builder.Property(e => e.TotalScore)
            .HasPrecision(10, 4);

        builder.Property(e => e.Aum)
            .HasPrecision(18, 2);

        builder.Property(e => e.ManagementFee)
            .HasPrecision(10, 4);

        builder.Property(e => e.MinimumInvestment)
            .HasMaxLength(200);

        builder.Property(e => e.Currency)
            .HasMaxLength(10);

        builder.Property(e => e.AssetManagerId);

        builder.Property(e => e.AssetManagerName)
            .HasMaxLength(250);

        builder.Property(e => e.AssetManagerNameEn)
            .HasMaxLength(250);

        builder.Property(e => e.AssetManagerCode)
            .HasMaxLength(50);

        builder.Property(e => e.LogoUrl)
            .HasMaxLength(500);

        builder.Property(e => e.LocalLogoPath)
            .HasMaxLength(500);

        // Free-text source fields: SNDUK descriptions exceed 2000 chars
        // (observed live truncation failure on DescriptionEn), so no max length.
        builder.Property(e => e.DescriptionAr)
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.DescriptionEn)
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.InvestmentStrategy)
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.InceptionDate);

        builder.Property(e => e.ProspectusUrl)
            .HasMaxLength(500);

        builder.Property(e => e.SndukCreatedAt)
            .IsRequired();

        builder.Property(e => e.SndukUpdatedAt)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        builder.Property(e => e.LastSyncedAt);

        builder.Property(e => e.PricesUpdatedAt);

        builder.HasMany(e => e.PriceHistory)
            .WithOne(p => p.Fund)
            .HasForeignKey(p => p.FundId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Analysis)
            .WithOne(a => a.Fund)
            .HasForeignKey<FundAnalysis>(a => a.FundId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.SimilarFunds)
            .WithOne(s => s.Fund)
            .HasForeignKey(s => s.FundId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.CategoryOverrides)
            .WithOne(o => o.Fund)
            .HasForeignKey(o => o.FundId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
