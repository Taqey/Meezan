using MediatR;
using Meezan.Application.Features.Shariah.Commands.RefreshShariahData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Meezan.Infrastructure.Services.BackgroundServices;

public class ShariahRefreshBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ShariahRefreshBackgroundService> _logger;
    // 3 months approx 90 days
    private static readonly TimeSpan Interval = TimeSpan.FromDays(90);

    public ShariahRefreshBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<ShariahRefreshBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ShariahRefreshBackgroundService started. Periodic interval: {Days} days", Interval.TotalDays);

        var nextRun = DateTime.UtcNow.Add(Interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Wait in smaller chunks (e.g., 1 hour or until nextRun) to avoid int.MaxValue milliseconds overflow
                var timeUntilNextRun = nextRun - DateTime.UtcNow;
                if (timeUntilNextRun > TimeSpan.Zero)
                {
                    var delayChunk = timeUntilNextRun > TimeSpan.FromHours(1) ? TimeSpan.FromHours(1) : timeUntilNextRun;
                    await Task.Delay(delayChunk, stoppingToken);
                    continue;
                }

                _logger.LogInformation("ShariahRefreshBackgroundService tick: triggering quarterly refresh");
                using var scope = _serviceProvider.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();

                var result = await sender.Send(new RefreshShariahDataCommand(TriggeredBy: "Scheduled"), stoppingToken);
                if (result.IsSuccess)
                {
                    _logger.LogInformation(
                        "Quarterly shariah refresh completed. Success: {Success}, PctUpdated: {PctUpdated}, StocksRefreshed: {Refreshed}",
                        result.Value.Success,
                        result.Value.PctUpdatedCount,
                        result.Value.StocksFullyRefreshedCount);
                }
                else
                {
                    _logger.LogWarning("Quarterly shariah refresh failed: {Error}", result.Error.Message);
                }

                nextRun = DateTime.UtcNow.Add(Interval);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in ShariahRefreshBackgroundService; run skipped without crashing app");
                // Wait 1 hour before retry on unexpected failure to avoid tight loop
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        _logger.LogInformation("ShariahRefreshBackgroundService stopping");
    }
}
