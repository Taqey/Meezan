using MediatR;
using Meezan.Application.Features.Scraping.Commands.RunCombinedScrape;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Meezan.Infrastructure.Services;

/// <summary>
/// Runs the live price + support/resistance scrape once per EGX trading day at 16:00 Cairo time
/// (Africa/Cairo = UTC+2, or UTC+3 during DST — we use the TimeZoneInfo API which handles this automatically).
/// </summary>
public class ScrapingBackgroundService : BackgroundService
{
    private static readonly TimeZoneInfo CairoTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time"); // Windows TZ id

    private static readonly TimeOnly RunTime = new(16, 0, 0); // 4:00 PM Cairo

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ScrapingBackgroundService> _logger;

    public ScrapingBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<ScrapingBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "ScrapingBackgroundService started. Daily run scheduled at {RunTime} Cairo time.", RunTime);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = ComputeDelayUntilNextRun();
            _logger.LogInformation(
                "Next scrape scheduled in {Minutes} minutes (at {RunTime} Cairo time).",
                (int)delay.TotalMinutes, RunTime);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            _logger.LogInformation("ScrapingBackgroundService: triggering daily live scrape run.");
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                var result = await sender.Send(
                    new RunCombinedScrapeCommand(
                        TriggeredBy: "Scheduled-LiveDaily",
                        Bucket: ScrapeDataBucket.LiveDaily),
                    stoppingToken);

                _logger.LogInformation(
                    "Daily scrape completed. Stocks: {Total}, Succeeded: {Succeeded}, Failed: {Failed}, Duration: {Duration}s",
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
                    "Unexpected error in ScrapingBackgroundService daily live run; will retry on the next EGX trading day.");
            }
        }

        _logger.LogInformation("ScrapingBackgroundService stopped.");
    }

    /// <summary>
    /// Computes how long to wait until the next 16:00 Cairo time on Sunday-Thursday.
    /// Friday and Saturday are skipped with no scrape attempt.
    /// </summary>
    private static TimeSpan ComputeDelayUntilNextRun()
    {
        var nowUtc = DateTime.UtcNow;
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
