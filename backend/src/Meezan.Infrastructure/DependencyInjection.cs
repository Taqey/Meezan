using Meezan.Application.Common.Interfaces;
using Meezan.Infrastructure.Persistence;
using Meezan.Infrastructure.Persistence.Repositories;
using Meezan.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Meezan.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IIndexRepository, IndexRepository>();
        services.AddScoped<IStockRepository, StockRepository>();
        services.AddScoped<ISectorRepository, SectorRepository>();
        services.AddScoped<IStockManagementReadQuery, Persistence.Queries.StockManagementReadQuery>();
        services.AddScoped<IAdminMarketDataReadQuery, Persistence.Queries.AdminMarketDataReadQuery>();
        services.AddScoped<IRemovalCandidatesReadQuery, Persistence.Queries.RemovalCandidatesReadQuery>();
        services.AddScoped<IUploadHistoryRepository, UploadHistoryRepository>();
        services.AddScoped<IJobSettingRepository, JobSettingRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IExcelParserService, ExcelParserService>();

        // Shariah Repositories
        services.AddScoped<IShariahComplianceRepository, ShariahComplianceRepository>();
        services.AddScoped<IShariahSourceOpinionRepository, ShariahSourceOpinionRepository>();
        services.AddScoped<IStockShariahMetricsRepository, StockShariahMetricsRepository>();
        services.AddScoped<IShariahRefreshLogRepository, ShariahRefreshLogRepository>();

        // Shariah External Client
        services.AddHttpClient<IShariahSourceClient, ShariahSourceClient>();

        // Shariah Scheduled Background Service
        services.AddHostedService<ShariahRefreshBackgroundService>();

        // Part 4 — market data & scraping repositories
        services.AddScoped<IStockMarketDataRepository, StockMarketDataRepository>();
        services.AddScoped<IStockFairValueRepository, StockFairValueRepository>();
        services.AddScoped<IStockSupportResistanceRepository, StockSupportResistanceRepository>();
        services.AddScoped<IScrapeRunLogRepository, ScrapeRunLogRepository>();
        services.AddScoped<IScrapeBatchCommitter, ScrapeBatchCommitter>();
        services.AddScoped<IMarketSnapshotRepository, MarketSnapshotRepository>();

        // Part 4 — singleton in-memory run progress tracker (transient, not persisted)
        services.AddSingleton<IScrapeProgressTracker, ScrapeProgressTracker>();

        // Persisted job schedule settings service (singleton with in-memory caching)
        services.AddSingleton<IJobSettingsService, JobSettingsService>();

        // Part 4 — Mubasher scraper client (named HttpClient)
        services.AddHttpClient<IStockScraperClient, MubasherScraperClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddHttpClient<IMubasherMarketSnapshotClient, MubasherMarketSnapshotClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Part 4 — Investing.com fallback scraper (used only when Mubasher fields are null)
        services.AddHttpClient<IInvestingScraperService, InvestingScraperClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Part 4 — background services
        services.AddHostedService<ScrapingBackgroundService>();
        services.AddHostedService<SlowMarketDataBackgroundService>();
        services.AddHostedService<MarketSnapshotsBackgroundService>();

        // Funds
        services.AddScoped<IFundRepository, FundRepository>();
        services.AddScoped<IFundPriceHistoryRepository, FundPriceHistoryRepository>();
        services.AddScoped<IFundAnalysisRepository, FundAnalysisRepository>();
        services.AddScoped<IFundSimilarRepository, FundSimilarRepository>();
        services.AddScoped<IFundCategoryRepository, FundCategoryRepository>();
        services.AddScoped<IFundCategoryOverrideRepository, FundCategoryOverrideRepository>();
        services.AddScoped<IFundSyncLogRepository, FundSyncLogRepository>();
        services.AddScoped<IFundSyncLogDetailRepository, FundSyncLogDetailRepository>();
        services.AddScoped<IFundSyncSettingsRepository, FundSyncSettingsRepository>();
        services.AddScoped<IFundCategoryChangeLogRepository, FundCategoryChangeLogRepository>();
        services.AddScoped<IFundClassificationNodeRepository, FundClassificationNodeRepository>();
        services.AddScoped<IFundClassificationOverrideRepository, FundClassificationOverrideRepository>();
        services.AddScoped<IFundClassificationChangeLogRepository, FundClassificationChangeLogRepository>();
        services.AddScoped<IFundClassificationBackupRepository, FundClassificationBackupRepository>();
        services.AddScoped<IFundClassificationService, FundClassificationService>();
        services.AddScoped<IFundReadQuery, FundReadQuery>();
        services.AddScoped<IFundTypeDefinitionRepository, FundTypeDefinitionRepository>();
        services.AddScoped<IFundTypeOverrideRepository, FundTypeOverrideRepository>();
        services.AddScoped<IFundTypeChangeLogRepository, FundTypeChangeLogRepository>();

        // SNDUK Fund Client
        services.AddHttpClient<ISndukFundClient, SndukFundClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Fund logo downloader. Opt-in via Funds:Logos:RootPath; when unset the storage
        // reports IsEnabled = false and Fund.LocalLogoPath stays empty (logos stay remote-only).
        services.Configure<FundLogoStorageOptions>(o =>
        {
            o.RootPath = configuration["Funds:Logos:RootPath"] ?? string.Empty;
            o.PublicBaseUrl = configuration["Funds:Logos:PublicBaseUrl"] ?? string.Empty;
            var max = configuration.GetValue<int?>("Funds:Logos:MaxBytes");
            if (max.HasValue && max.Value > 0) o.MaxBytes = max.Value;
        });
        services.AddHttpClient<IFundLogoStorage, FundLogoStorage>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddSingleton<IFundAutoClassifier, FundAutoClassifier>();

        // Fund Sync Background Service (the sync itself is RunFundSyncCommand in Application)
        services.Configure<FundSyncOptions>(configuration.GetSection(FundSyncOptions.SectionName));
        services.AddHostedService<FundSyncBackgroundService>();

        // Parallel loader for the fund detail page (own scopes: a DbContext is not thread-safe)
        services.AddScoped<IFundDetailQuery, FundDetailQuery>();
        services.AddScoped<FundDetailQuery>();

        return services;
    }
}
