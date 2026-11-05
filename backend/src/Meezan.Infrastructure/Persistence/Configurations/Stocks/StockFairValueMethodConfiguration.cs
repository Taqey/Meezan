using Meezan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Meezan.Infrastructure.Persistence.Configurations.Stocks;

public class StockFairValueMethodConfiguration : IEntityTypeConfiguration<StockFairValueMethod>
{
    public void Configure(EntityTypeBuilder<StockFairValueMethod> builder)
    {
        builder.ToTable("StockFairValueMethods");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.MethodName).IsRequired().HasMaxLength(50);
        builder.Property(m => m.EstimatedValue).HasColumnType("decimal(18,4)");

        builder.HasOne(m => m.StockFairValue)
            .WithMany(fv => fv.Methods)
            .HasForeignKey(m => m.StockFairValueId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
