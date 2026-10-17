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
using Meezan.Application.Features.Scraping.Commands.RunCombinedScrape;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Meezan.Infrastructure.Services;

/// <summary>
/// Runs the slow-changing Mubasher fundamentals scrape quarterly.
/// Default trigger: first day of Jan/Apr/Jul/Oct at 09:00 Cairo time.
/// Egyptian public-holiday skipping is a future improvement, not implemented here.
/// </summary>
public class SlowMarketDataBackgroundService : BackgroundService
{
    private static readonly TimeZoneInfo CairoTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time"); // Windows TZ id

    private static readonly int[] QuarterStartMonths = [1, 4, 7, 10];
    private static readonly TimeOnly RunTime = new(9, 0, 0);

    private readonly IServiceProvider _serviceProvider;
    private readonly IJobSettingsService _jobSettings;
    private readonly ILogger<SlowMarketDataBackgroundService> _logger;

    public SlowMarketDataBackgroundService(
        IServiceProvider serviceProvider,
        IJobSettingsService jobSettings,
        ILogger<SlowMarketDataBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _jobSettings = jobSettings;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "SlowMarketDataBackgroundService started. Quarterly run scheduled for day 1 of Jan/Apr/Jul/Oct at {RunTime} Cairo time.",
            RunTime);

        while (!stoppingToken.IsCancellationRequested)
        {
            // ── Wait until the next quarterly trigger ───────────────────────────
            // Task.Delay's maximum is Int32.MaxValue ms (~24.8 days). A quarterly
            // delay (~3 months) overflows that limit with ArgumentOutOfRangeException.
            // Wait in 1-hour chunks instead, exactly like ShariahRefreshBackgroundService.
            var targetUtc = ComputeNextRunUtc();
            _logger.LogInformation(
                "Next slow market-data scrape scheduled at {Target} UTC ({Minutes} minutes from now).",
                targetUtc, (int)(targetUtc - DateTime.UtcNow).TotalMinutes);

            while (DateTime.UtcNow < targetUtc && !stoppingToken.IsCancellationRequested)
            {
                var remaining = targetUtc - DateTime.UtcNow;
                var chunk = remaining > TimeSpan.FromHours(1) ? TimeSpan.FromHours(1) : remaining;
                try
                {
                    await Task.Delay(chunk, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
            }

            if (stoppingToken.IsCancellationRequested) break;

            if (!_jobSettings.IsEnabled("SlowQuarterly"))
            {
                _logger.LogInformation("SlowMarketDataBackgroundService: scheduled run skipped because SlowQuarterly job is disabled.");
                continue;
            }

            _logger.LogInformation("SlowMarketDataBackgroundService: triggering quarterly slow scrape run.");
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                var result = await sender.Send(
                    new RunCombinedScrapeCommand(
                        TriggeredBy: "Scheduled-SlowQuarterly",
                        Bucket: ScrapeDataBucket.SlowQuarterly),
                    stoppingToken);

                _logger.LogInformation(
                    "Quarterly slow scrape completed. Stocks: {Total}, Succeeded: {Succeeded}, Failed: {Failed}, Duration: {Duration}s",
                    result.TotalStocks, result.Succeeded, result.Failed,
                    (int)result.Duration.TotalSeconds);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error in SlowMarketDataBackgroundService quarterly run; will retry at the next quarterly trigger.");
            }
        }

        _logger.LogInformation("SlowMarketDataBackgroundService stopped.");
    }

    private static DateTime ComputeNextRunUtc()
    {
        var nowUtc = DateTime.UtcNow;
        var nowCairo = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, CairoTimeZone);

        foreach (var month in QuarterStartMonths.Where(m => m >= nowCairo.Month))
        {
            var candidate = new DateTime(nowCairo.Year, month, 1) + RunTime.ToTimeSpan();
            if (nowCairo < candidate)
            {
                return TimeZoneInfo.ConvertTimeToUtc(candidate, CairoTimeZone);
            }
        }

        var nextYearCandidate = new DateTime(nowCairo.Year + 1, QuarterStartMonths[0], 1) + RunTime.ToTimeSpan();
        return TimeZoneInfo.ConvertTimeToUtc(nextYearCandidate, CairoTimeZone);
    }
}
