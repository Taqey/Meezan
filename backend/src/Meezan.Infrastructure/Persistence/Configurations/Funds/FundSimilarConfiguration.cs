using Meezan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Meezan.Infrastructure.Persistence.Configurations.Funds;

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
