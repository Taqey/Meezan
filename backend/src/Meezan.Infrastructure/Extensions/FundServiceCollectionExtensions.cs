using Meezan.Application.Common.Interfaces.Integrations;
using Meezan.Application.Common.Interfaces.Queries.Funds;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Interfaces.Storage;
using Meezan.Infrastructure.Persistence.Queries.Funds;
using Meezan.Infrastructure.Persistence.Repositories.Funds;
using Meezan.Infrastructure.Services.BackgroundServices;
using Meezan.Infrastructure.Services.Funds.Classification;
using Meezan.Infrastructure.Services.Funds.Clients;
using Meezan.Infrastructure.Services.Funds.Queries;
using Meezan.Infrastructure.Services.Funds.Storage;
using Meezan.Infrastructure.Services.Funds.Sync;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Meezan.Infrastructure.Extensions;

public static class FundServiceCollectionExtensions
{
    public static IServiceCollection AddFundServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Funds Repositories
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

        // Fund Sync Background Service
        services.AddHostedService<FundSyncBackgroundService>();
        services.AddScoped<IFundSyncService, FundSyncService>();

        // Parallel loader for the fund detail page (own scopes: a DbContext is not thread-safe)
        services.AddScoped<IFundDetailQuery, FundDetailQuery>();
        services.AddScoped<FundDetailQuery>();

        return services;
    }
}
