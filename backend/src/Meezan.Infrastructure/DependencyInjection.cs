using Meezan.Application.Common.Interfaces;
using Meezan.Infrastructure.Persistence;
using Meezan.Infrastructure.Persistence.Repositories;
using Meezan.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
        services.AddScoped<IUploadHistoryRepository, UploadHistoryRepository>();
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

        // Part 4 — singleton in-memory run progress tracker (transient, not persisted)
        services.AddSingleton<IScrapeProgressTracker, ScrapeProgressTracker>();

        // Part 4 — Mubasher scraper client (named HttpClient)
        services.AddHttpClient<IStockScraperClient, MubasherScraperClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Part 4 — Investing.com fallback scraper (used only when Mubasher fields are null)
        services.AddHttpClient<IInvestingScraperService, InvestingScraperClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Part 4 — daily scraping background service (fires at 16:00 Cairo time)
        services.AddHostedService<ScrapingBackgroundService>();
        services.AddHostedService<SlowMarketDataBackgroundService>();

        return services;
    }
}
