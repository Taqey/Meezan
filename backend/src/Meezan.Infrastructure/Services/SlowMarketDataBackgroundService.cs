using MediatR;
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
    private readonly ILogger<SlowMarketDataBackgroundService> _logger;

    public SlowMarketDataBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<SlowMarketDataBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "SlowMarketDataBackgroundService started. Quarterly run scheduled for day 1 of Jan/Apr/Jul/Oct at {RunTime} Cairo time.",
            RunTime);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = ComputeDelayUntilNextRun();
            _logger.LogInformation(
                "Next slow market-data scrape scheduled in {Minutes} minutes.",
                (int)delay.TotalMinutes);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
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

    private static TimeSpan ComputeDelayUntilNextRun()
    {
        var nowUtc = DateTime.UtcNow;
        var nowCairo = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, CairoTimeZone);

        foreach (var month in QuarterStartMonths.Where(m => m >= nowCairo.Month))
        {
            var candidate = new DateTime(nowCairo.Year, month, 1) + RunTime.ToTimeSpan();
            if (nowCairo < candidate)
            {
                return ToDelay(candidate, nowUtc);
            }
        }

        var nextYearCandidate = new DateTime(nowCairo.Year + 1, QuarterStartMonths[0], 1) + RunTime.ToTimeSpan();
        return ToDelay(nextYearCandidate, nowUtc);
    }

    private static TimeSpan ToDelay(DateTime cairoTime, DateTime nowUtc)
    {
        var nextRunUtc = TimeZoneInfo.ConvertTimeToUtc(cairoTime, CairoTimeZone);
        var delay = nextRunUtc - nowUtc;
        return delay < TimeSpan.Zero ? TimeSpan.Zero : delay;
    }
}
