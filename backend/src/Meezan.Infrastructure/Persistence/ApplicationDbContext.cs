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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
