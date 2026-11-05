using Meezan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Meezan.Infrastructure.Persistence.Configurations.Funds;

public class FundTypeDefinitionConfiguration : IEntityTypeConfiguration<FundTypeDefinition>
{
    public void Configure(EntityTypeBuilder<FundTypeDefinition> builder)
    {
        builder.ToTable("FundTypeDefinitions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Slug).IsRequired().HasMaxLength(100);
        builder.Property(e => e.NameAr).IsRequired().HasMaxLength(200);
        builder.Property(e => e.NameEn).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Icon).IsRequired().HasMaxLength(50);
        builder.Property(e => e.Color).IsRequired().HasMaxLength(20);
        builder.Property(e => e.DisplayOrder).IsRequired();
        builder.Property(e => e.IsVisible).IsRequired();
        builder.Property(e => e.IsDeleted).IsRequired();
        builder.Property(e => e.DeletedReason).IsRequired().HasMaxLength(500);
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();
        builder.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
        builder.Property(e => e.UpdatedBy).IsRequired().HasMaxLength(100);

        builder.HasIndex(e => e.Slug).IsUnique();
        builder.HasIndex(e => e.DisplayOrder);
    }
}
