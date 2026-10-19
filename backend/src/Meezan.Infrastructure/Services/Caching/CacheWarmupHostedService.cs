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

    private Task? _warmupTask;
    private CancellationTokenSource? _stoppingCts;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var ct = _stoppingCts.Token;

        _warmupTask = Task.Run(async () =>
        {
            try
            {
                // Delayed start: give the host a moment to complete startup before warm-up
                await Task.Delay(2000, ct);
                if (ct.IsCancellationRequested) return;

                using var scope = _services.CreateScope();
                var options = scope.ServiceProvider.GetService<Microsoft.Extensions.Options.IOptions<Meezan.Application.Common.Models.Caching.FundCacheOptions>>()?.Value;
                if (options != null && (!options.Enabled || !options.WarmupOnStartup))
                {
                    _logger.LogInformation("Fund cache warmup skipped per configuration (Enabled={Enabled}, WarmupOnStartup={WarmupOnStartup}).", options.Enabled, options.WarmupOnStartup);
                    return;
                }

                var cache = scope.ServiceProvider.GetRequiredService<IFundCacheService>();
                _logger.LogInformation("Warming up fund cache in the background...");
                await cache.GetClassificationDatasetAsync(ct);
                await cache.GetStaticFundsAsync(ct);
                _logger.LogInformation("Fund cache background warmup completed successfully.");
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Background fund cache warmup was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Background fund cache warmup encountered an error (continuing without blocking startup).");
            }
        }, ct);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_stoppingCts != null)
        {
            _stoppingCts.Cancel();
        }

        if (_warmupTask != null)
        {
            try
            {
                await Task.WhenAny(_warmupTask, Task.Delay(Timeout.Infinite, cancellationToken));
            }
            catch (OperationCanceledException)
            {
                // Host shutdown timed out or was cancelled
            }
        }
    }
}
