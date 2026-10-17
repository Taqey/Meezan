using Meezan.Application.Common.Interfaces.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Meezan.Infrastructure.Services.Caching;

public class CacheWarmupHostedService : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<CacheWarmupHostedService> _logger;

    public CacheWarmupHostedService(IServiceProvider services, ILogger<CacheWarmupHostedService> logger)
    {
        _services = services;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _services.CreateScope();
                var cache = scope.ServiceProvider.GetRequiredService<IFundCacheService>();
                _logger.LogInformation("Warming up fund cache in the background...");
                await cache.GetClassificationDatasetAsync(CancellationToken.None);
                await cache.GetStaticFundsAsync(CancellationToken.None);
                await cache.GetPricesSnapshotAsync(CancellationToken.None);
                _logger.LogInformation("Fund cache background warmup completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Background fund cache warmup encountered an error (continuing without blocking startup).");
            }
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
