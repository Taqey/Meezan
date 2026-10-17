using MediatR;
using Meezan.Application.Common.Interfaces.Integrations;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Interfaces.Queries.Funds;
using Meezan.Application.Common.Interfaces.Queries.RemovalCandidates;
using Meezan.Application.Common.Interfaces.Queries.Stocks;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Interfaces.Repositories.Indices;
using Meezan.Application.Common.Interfaces.Repositories.Market;
using Meezan.Application.Common.Interfaces.Repositories.Scraping;
using Meezan.Application.Common.Interfaces.Repositories.Sectors;
using Meezan.Application.Common.Interfaces.Repositories.Shariah;
using Meezan.Application.Common.Interfaces.Repositories.Stocks;
using Meezan.Application.Common.Interfaces.Repositories.System;
using Meezan.Application.Common.Interfaces.Security;
using Meezan.Application.Common.Interfaces.Storage;
using Meezan.Application.Features.Market.Commands.RefreshMarketSnapshots;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Meezan.Infrastructure.Services.BackgroundServices;

/// <summary>
/// Runs the Mubasher market snapshots scrape (indices + sectors quotes) once per EGX trading day
/// at 16:00 Cairo time (aligned with the Daily Live-Price schedule).
/// Friday and Saturday are skipped with no scrape attempt.
/// </summary>
public class MarketSnapshotsBackgroundService : BackgroundService
{
    private static readonly TimeZoneInfo CairoTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time"); // Windows TZ id

    private static readonly TimeOnly RunTime = new(16, 0, 0); // 4:00 PM Cairo

    private readonly IServiceProvider _serviceProvider;
    private readonly IJobSettingsService _jobSettings;
    private readonly ILogger<MarketSnapshotsBackgroundService> _logger;

    public MarketSnapshotsBackgroundService(
        IServiceProvider serviceProvider,
        IJobSettingsService jobSettings,
        ILogger<MarketSnapshotsBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _jobSettings = jobSettings;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "MarketSnapshotsBackgroundService started. Daily run scheduled at {RunTime} Cairo time.", RunTime);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = ComputeDelayUntilNextRun();
            _logger.LogInformation(
                "Next market snapshots scrape scheduled in {Minutes} minutes (at {RunTime} Cairo time).",
                (int)delay.TotalMinutes, RunTime);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            if (!_jobSettings.IsEnabled("MarketSnapshots"))
            {
                _logger.LogInformation(
                    "MarketSnapshotsBackgroundService: scheduled run skipped because MarketSnapshots job is disabled.");
                continue;
            }

            _logger.LogInformation("MarketSnapshotsBackgroundService: triggering daily snapshots refresh run.");
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                var result = await sender.Send(new RefreshMarketSnapshotsCommand(), stoppingToken);

                if (result.IsSuccess)
                {
                    _logger.LogInformation(
                        "Daily market snapshots refresh completed. Succeeded: {Succeeded}, Failed: {Failed}, Total: {Total}, Duration: {Duration}s",
                        result.Value.Succeeded, result.Value.Failed, result.Value.TotalTargets,
                        (int)result.Value.Duration.TotalSeconds);
                }
                else
                {
                    _logger.LogWarning("Daily market snapshots refresh failed: {Error}", result.Error.Message);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error in MarketSnapshotsBackgroundService daily run; will retry on next EGX trading day.");
            }
        }

        _logger.LogInformation("MarketSnapshotsBackgroundService stopped.");
    }

    /// <summary>
    /// Computes how long to wait until the next 16:00 Cairo time on Sunday-Thursday.
    /// Friday and Saturday are skipped with no scrape attempt.
    /// </summary>
    public static TimeSpan ComputeDelayUntilNextRun(DateTime? fromUtc = null)
    {
        var nowUtc = fromUtc ?? DateTime.UtcNow;
        var nowCairo = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, CairoTimeZone);

        var nextRunCairo = nowCairo.Date + RunTime.ToTimeSpan();
        if (nowCairo >= nextRunCairo)
        {
            nextRunCairo = nextRunCairo.AddDays(1);
        }

        while (nextRunCairo.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday)
        {
            nextRunCairo = nextRunCairo.AddDays(1);
        }

        var nextRunUtc = TimeZoneInfo.ConvertTimeToUtc(nextRunCairo, CairoTimeZone);
        var delay = nextRunUtc - nowUtc;

        // Guard: should never be negative, but clamp to 0 just in case
        return delay < TimeSpan.Zero ? TimeSpan.Zero : delay;
    }
}
