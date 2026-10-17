using Meezan.Application.Common.Interfaces.Integrations;
using Meezan.Application.Common.Interfaces.Repositories.Shariah;
using Meezan.Application.Common.Interfaces.Repositories.Stocks;
using Meezan.Application.Common.Interfaces.Storage;
using Meezan.Infrastructure.Persistence.Repositories.Shariah;
using Meezan.Infrastructure.Persistence.Repositories.Stocks;
using Meezan.Infrastructure.Services.BackgroundServices;
using Meezan.Infrastructure.Services.Shariah.Clients;
using Meezan.Infrastructure.Services.Shariah.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Meezan.Infrastructure.Extensions;

public static class ShariahServiceCollectionExtensions
{
    public static IServiceCollection AddShariahServices(this IServiceCollection services)
    {
        // Shariah Repositories
        services.AddScoped<IShariahComplianceRepository, ShariahComplianceRepository>();
        services.AddScoped<IShariahSourceOpinionRepository, ShariahSourceOpinionRepository>();
        services.AddScoped<IStockShariahMetricsRepository, StockShariahMetricsRepository>();
        services.AddScoped<IShariahRefreshLogRepository, ShariahRefreshLogRepository>();
        services.AddScoped<IShariahSourcePdfStorage, ShariahSourcePdfStorage>();
        services.AddScoped<IShariahMapLoader, ShariahMapLoader>();

        // Shariah External Client
        services.AddHttpClient<IShariahSourceClient, ShariahSourceClient>();

        // Shariah Scheduled Background Service
        services.AddHostedService<ShariahRefreshBackgroundService>();

        return services;
    }
}
