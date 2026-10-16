using Meezan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Meezan.Infrastructure.Persistence.Configurations;

public class FundAnalysisConfiguration : IEntityTypeConfiguration<FundAnalysis>
{
    public void Configure(EntityTypeBuilder<FundAnalysis> builder)
    {
        builder.ToTable("FundAnalyses");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.FundId)
            .IsRequired();

        builder.HasIndex(e => e.FundId)
            .IsUnique();

        builder.Property(e => e.PerformanceScore)
            .HasPrecision(10, 4);

        builder.Property(e => e.RiskScore)
            .HasPrecision(10, 4);

        builder.Property(e => e.CostScore)
            .HasPrecision(10, 4);

        builder.Property(e => e.StabilityScore)
            .HasPrecision(10, 4);

        builder.Property(e => e.DiversificationScore)
            .HasPrecision(10, 4);

        builder.Property(e => e.TotalScore)
            .HasPrecision(10, 4);

        builder.Property(e => e.Cagr)
            .HasPrecision(10, 4);

        builder.Property(e => e.Alpha)
            .HasPrecision(10, 4);

        builder.Property(e => e.Volatility)
            .HasPrecision(10, 4);

        builder.Property(e => e.Consistency)
            .HasPrecision(10, 4);

        builder.Property(e => e.MaxDrawdown)
            .HasPrecision(10, 4);

        builder.Property(e => e.SharpeRatio)
            .HasPrecision(10, 4);

        builder.Property(e => e.WeightsJson)
            .HasMaxLength(2000);

        builder.Property(e => e.PerformanceExplanationAr)
            .HasMaxLength(1000);

        builder.Property(e => e.PerformanceExplanationEn)
            .HasMaxLength(1000);

        builder.Property(e => e.RiskExplanationAr)
            .HasMaxLength(1000);

        builder.Property(e => e.RiskExplanationEn)
            .HasMaxLength(1000);

        builder.Property(e => e.CostExplanationAr)
            .HasMaxLength(1000);

        builder.Property(e => e.CostExplanationEn)
            .HasMaxLength(1000);

        builder.Property(e => e.StabilityExplanationAr)
            .HasMaxLength(1000);

        builder.Property(e => e.StabilityExplanationEn)
            .HasMaxLength(1000);

        builder.Property(e => e.DiversificationExplanationAr)
            .HasMaxLength(1000);

        builder.Property(e => e.DiversificationExplanationEn)
            .HasMaxLength(1000);

        builder.Property(e => e.CalculatedAt)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        builder.HasOne(e => e.Fund)
            .WithOne(f => f.Analysis)
            .HasForeignKey<FundAnalysis>(a => a.FundId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class FundPriceHistoryConfiguration : IEntityTypeConfiguration<FundPriceHistory>
{
    public void Configure(EntityTypeBuilder<FundPriceHistory> builder)
    {
        builder.ToTable("FundPriceHistory");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.FundId)
            .IsRequired();

        builder.HasIndex(e => new { e.FundId, e.Date })
            .IsUnique();

        builder.Property(e => e.Date)
            .IsRequired();

        builder.Property(e => e.Price)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(e => e.Change)
            .HasPrecision(10, 4);

        builder.Property(e => e.Performance)
            .HasPrecision(10, 4);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.HasOne(e => e.Fund)
            .WithMany(f => f.PriceHistory)
            .HasForeignKey(e => e.FundId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class FundSimilarConfiguration : IEntityTypeConfiguration<FundSimilar>
{
    public void Configure(EntityTypeBuilder<FundSimilar> builder)
    {
        builder.ToTable("FundSimilars");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.FundId)
            .IsRequired();

        builder.Property(e => e.SimilarFundId)
            .IsRequired();

        builder.HasIndex(e => new { e.FundId, e.SimilarFundId })
            .IsUnique();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.HasOne(e => e.Fund)
            .WithMany(f => f.SimilarFunds)
            .HasForeignKey(e => e.FundId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.SimilarFund)
            .WithMany()
            .HasForeignKey(e => e.SimilarFundId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}