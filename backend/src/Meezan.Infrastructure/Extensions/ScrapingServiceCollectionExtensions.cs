using Meezan.Application.Common.Interfaces.Integrations;
using Meezan.Application.Common.Interfaces.Repositories.Market;
using Meezan.Application.Common.Interfaces.Repositories.Scraping;
using Meezan.Application.Common.Interfaces.Repositories.Stocks;
using Meezan.Application.Common.Interfaces.Repositories.System;
using Meezan.Infrastructure.Persistence.Repositories.Market;
using Meezan.Infrastructure.Persistence.Repositories.Scraping;
using Meezan.Infrastructure.Persistence.Repositories.Stocks;
using Meezan.Infrastructure.Services.BackgroundServices;
using Meezan.Infrastructure.Services.Jobs;
using Meezan.Infrastructure.Services.Scraping.Clients;
using Meezan.Infrastructure.Services.Scraping.Processing;
using Microsoft.Extensions.DependencyInjection;

namespace Meezan.Infrastructure.Extensions;

public static class ScrapingServiceCollectionExtensions
{
    public static IServiceCollection AddScrapingServices(this IServiceCollection services)
    {
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

        return services;
    }
}
