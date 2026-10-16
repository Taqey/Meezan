using Meezan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Index = Meezan.Domain.Entities.Index;

namespace Meezan.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Index> Indices => Set<Index>();
    public DbSet<Sector> Sectors => Set<Sector>();
    public DbSet<Stock> Stocks => Set<Stock>();
    public DbSet<IndexConstituent> IndexConstituents => Set<IndexConstituent>();
    public DbSet<UploadHistory> UploadHistories => Set<UploadHistory>();
    public DbSet<ShariahCompliance> ShariahCompliances => Set<ShariahCompliance>();
    public DbSet<ShariahSourceOpinion> ShariahSourceOpinions => Set<ShariahSourceOpinion>();
    public DbSet<StockShariahMetrics> StockShariahMetrics => Set<StockShariahMetrics>();
    public DbSet<ShariahRefreshLog> ShariahRefreshLogs => Set<ShariahRefreshLog>();

    // Part 4 — market data & scraping
    public DbSet<StockMarketData> StockMarketData => Set<StockMarketData>();
    public DbSet<StockFairValue> StockFairValues => Set<StockFairValue>();
    public DbSet<StockFairValueMethod> StockFairValueMethods => Set<StockFairValueMethod>();
    public DbSet<StockSupportResistance> StockSupportResistance => Set<StockSupportResistance>();
    public DbSet<ScrapeRunLog> ScrapeRunLogs => Set<ScrapeRunLog>();

    // Published market snapshots (Mubasher index/sector quotes cache)
    public DbSet<MarketIndexSnapshot> MarketIndexSnapshots => Set<MarketIndexSnapshot>();

    // Job enabled/disabled flags (persisted across restarts)
    public DbSet<JobSetting> JobSettings => Set<JobSetting>();

    // Funds
    public DbSet<Fund> Funds => Set<Fund>();
    public DbSet<FundPriceHistory> FundPriceHistories => Set<FundPriceHistory>();
    public DbSet<FundAnalysis> FundAnalyses => Set<FundAnalysis>();
    public DbSet<FundSimilar> FundSimilars => Set<FundSimilar>();
    public DbSet<FundCategory> FundCategories => Set<FundCategory>();
    public DbSet<FundCategoryOverride> FundCategoryOverrides => Set<FundCategoryOverride>();
    public DbSet<FundSyncLog> FundSyncLogs => Set<FundSyncLog>();
    public DbSet<FundSyncLogDetail> FundSyncLogDetails => Set<FundSyncLogDetail>();
    public DbSet<FundSyncSettings> FundSyncSettings => Set<FundSyncSettings>();
    public DbSet<FundCategoryChangeLog> FundCategoryChangeLogs => Set<FundCategoryChangeLog>();
    public DbSet<FundTypeDefinition> FundTypeDefinitions => Set<FundTypeDefinition>();
    public DbSet<FundTypeOverride> FundTypeOverrides => Set<FundTypeOverride>();
    public DbSet<FundTypeChangeLog> FundTypeChangeLogs => Set<FundTypeChangeLog>();

    // Classification tree
    public DbSet<FundClassificationNode> FundClassificationNodes => Set<FundClassificationNode>();
    public DbSet<FundClassificationOverride> FundClassificationOverrides => Set<FundClassificationOverride>();
    public DbSet<FundClassificationChangeLog> FundClassificationChangeLogs => Set<FundClassificationChangeLog>();
    public DbSet<FundClassificationBackup> FundClassificationBackups => Set<FundClassificationBackup>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        modelBuilder.Entity<FundTypeDefinition>().HasData(
            new[]
            {
                new FundTypeDefinition { Id = 1, Slug = "equity",               NameAr = "صندوق أسهم",              NameEn = "Equity Fund",           Icon = "", Color = "", DisplayOrder = 1,  IsVisible = true,  IsDeleted = false, DeletedAt = null, DeletedReason = "", CreatedAt = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc), CreatedBy = "Seed", UpdatedBy = "Seed" },
                new FundTypeDefinition { Id = 2, Slug = "fixed_income",         NameAr = "صندوق دخل ثابت",          NameEn = "Fixed Income Fund",     Icon = "", Color = "", DisplayOrder = 2,  IsVisible = true,  IsDeleted = false, DeletedAt = null, DeletedReason = "", CreatedAt = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc), CreatedBy = "Seed", UpdatedBy = "Seed" },
                new FundTypeDefinition { Id = 3, Slug = "fixed_income_usd",     NameAr = "صندوق دخل ثابت بالدولار", NameEn = "USD Fixed Income Fund", Icon = "", Color = "", DisplayOrder = 3,  IsVisible = true,  IsDeleted = false, DeletedAt = null, DeletedReason = "", CreatedAt = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc), CreatedBy = "Seed", UpdatedBy = "Seed" },
                new FundTypeDefinition { Id = 4, Slug = "balanced",             NameAr = "صندوق متوازن",            NameEn = "Balanced Fund",         Icon = "", Color = "", DisplayOrder = 4,  IsVisible = true,  IsDeleted = false, DeletedAt = null, DeletedReason = "", CreatedAt = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc), CreatedBy = "Seed", UpdatedBy = "Seed" },
                new FundTypeDefinition { Id = 5, Slug = "money_market",         NameAr = "صندوق سوق نقدي",          NameEn = "Money Market Fund",     Icon = "", Color = "", DisplayOrder = 5,  IsVisible = true,  IsDeleted = false, DeletedAt = null, DeletedReason = "", CreatedAt = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc), CreatedBy = "Seed", UpdatedBy = "Seed" },
                new FundTypeDefinition { Id = 6, Slug = "index",                NameAr = "صندوق مؤشر",              NameEn = "Index Fund",            Icon = "", Color = "", DisplayOrder = 6,  IsVisible = true,  IsDeleted = false, DeletedAt = null, DeletedReason = "", CreatedAt = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc), CreatedBy = "Seed", UpdatedBy = "Seed" },
                new FundTypeDefinition { Id = 7, Slug = "gold",                 NameAr = "صندوق ذهب",               NameEn = "Gold Fund",             Icon = "", Color = "", DisplayOrder = 7,  IsVisible = true,  IsDeleted = false, DeletedAt = null, DeletedReason = "", CreatedAt = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc), CreatedBy = "Seed", UpdatedBy = "Seed" },
                new FundTypeDefinition { Id = 8, Slug = "commodities",          NameAr = "صندوق سلع",               NameEn = "Commodities Fund",      Icon = "", Color = "", DisplayOrder = 8,  IsVisible = true,  IsDeleted = false, DeletedAt = null, DeletedReason = "", CreatedAt = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc), CreatedBy = "Seed", UpdatedBy = "Seed" },
                new FundTypeDefinition { Id = 9, Slug = "capital_preservation", NameAr = "صندوق الحفاظ على رأس المال", NameEn = "Capital Preservation Fund", Icon = "", Color = "", DisplayOrder = 9,  IsVisible = true,  IsDeleted = false, DeletedAt = null, DeletedReason = "", CreatedAt = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc), CreatedBy = "Seed", UpdatedBy = "Seed" },
                new FundTypeDefinition { Id = 10, Slug = "unknown",             NameAr = "غير محدد",                NameEn = "Unclassified",          Icon = "", Color = "", DisplayOrder = 99, IsVisible = true,  IsDeleted = false, DeletedAt = null, DeletedReason = "", CreatedAt = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc), CreatedBy = "Seed", UpdatedBy = "Seed" }
            });
    }
}
