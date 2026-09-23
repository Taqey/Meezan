using Meezan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Index = Meezan.Domain.Entities.Index;

namespace Meezan.Infrastructure.Persistence.Configurations;

public class IndexConfiguration : IEntityTypeConfiguration<Index>
{
    public void Configure(EntityTypeBuilder<Index> builder)
    {
        builder.ToTable("Indices");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(e => e.Code)
            .IsUnique();

        builder.Property(e => e.NameAr)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(e => e.NameEn)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.LastUpdated);

        // Seed data
        builder.HasData(
            new Index { Id = 1, Code = "EGX30", NameAr = "المؤشر الرئيسي للبورصة المصرية EGX30", NameEn = "EGX 30 Index", Description = "Top 30 liquid and active companies" },
            new Index { Id = 2, Code = "EGX30TR", NameAr = "مؤشر العائد الكلي EGX30TR", NameEn = "EGX 30 Total Return Index", Description = "EGX 30 Total Return Index including dividend reinvestment" },
            new Index { Id = 3, Code = "EGX70", NameAr = "مؤشر الشركات الصغيرة والمتوسطة EGX70 EWI", NameEn = "EGX 70 EWI Index", Description = "EGX 70 Equal Weights Index" },
            new Index { Id = 4, Code = "EGX100", NameAr = "المؤشر الأوسع نطاقاً EGX100 EWI", NameEn = "EGX 100 EWI Index", Description = "EGX 100 Equal Weights Index" },
            new Index { Id = 5, Code = "EGX35-LV", NameAr = "مؤشر التذبذب المنخفض EGX35-LV", NameEn = "EGX 35 Low Volatility Index", Description = "EGX 35 Low Volatility Index" },
            new Index { Id = 6, Code = "Shariah", NameAr = "مؤشر الشريعة EGX30 Shariah", NameEn = "EGX 33 Shariah Index", Description = "Shariah Compliant Index" },
            new Index { Id = 7, Code = "Sectoral-Indices", NameAr = "المؤشرات القطاعية", NameEn = "Sectoral Indices", Description = "EGX Sectoral Indices Constituents" },
            new Index { Id = 8, Code = "TAMAYUZ", NameAr = "مؤشر سوق الشركات الصغيرة والمتوسطة تميز", NameEn = "Tamayuz Index", Description = "Tamayuz Small & Medium Enterprises Index" }
        );
    }
}
