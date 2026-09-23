using MediatR;
using Meezan.Application.Common.Interfaces;
using Meezan.Application.Features.Scraping.Commands.RunCombinedScrape;
using Microsoft.AspNetCore.Mvc;

namespace Meezan.WebApi.Controllers;

[ApiController]
[Route("api/scraping")]
public class ScrapingController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IScrapeProgressTracker _progress;
    private readonly IScrapeRunLogRepository _runLogRepo;

    public ScrapingController(
        ISender sender,
        IScrapeProgressTracker progress,
        IScrapeRunLogRepository runLogRepo)
    {
        _sender = sender;
        _progress = progress;
        _runLogRepo = runLogRepo;
    }

    /// <summary>
    /// Manually triggers a full market-data + support/resistance scrape for all stocks.
    /// This is a long-running operation — it runs synchronously and returns when complete.
    /// </summary>
    [HttpPost("run")]
    [ProducesResponseType(typeof(RunCombinedScrapeResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> RunScrape(
        [FromQuery] ScrapeDataBucket bucket = ScrapeDataBucket.LiveDaily,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new RunCombinedScrapeCommand(
                TriggeredBy: bucket == ScrapeDataBucket.LiveDaily ? "Manual-LiveDaily" : "Manual-SlowQuarterly",
                Bucket: bucket),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Returns the live progress of an active scrape run, or a summary of the
    /// last completed run when no run is in progress.
    ///
    /// Designed for polling every 2–3 seconds to drive a frontend progress bar.
    /// Response time: O(1) — reads in-memory state only; no DB query on the hot path
    /// during an active run. A single lightweight DB query is made only when idle
    /// (status = NotRunning/Completed/Failed) to populate the last-run summary.
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(ScrapeStatusResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var snapshot = _progress.GetSnapshot();

        // While a run is actively in-flight, return the live state immediately.
        // No DB touch — response time is O(1).
        bool isActive = snapshot.Status is ScrapeRunStatus.Running or ScrapeRunStatus.Committing;
        if (isActive)
        {
            return Ok(new ScrapeStatusResponse(
                Status: snapshot.Status.ToString(),
                Live: MapLive(snapshot),
                LastRun: null
            ));
        }

        // When idle / completed / failed, also pull the last persisted run summary.
        // One lightweight query — only happens when the frontend is NOT polling rapidly.
        var lastLog = await _runLogRepo.GetLatestAsync(cancellationToken);
        LastRunSummary? lastRun = lastLog is null ? null : new LastRunSummary(
            RunAt: lastLog.StartedAt,
            FinishedAt: lastLog.FinishedAt,
            DurationSeconds: lastLog.FinishedAt.HasValue
                ? (int)(lastLog.FinishedAt.Value - lastLog.StartedAt).TotalSeconds
                : null,
            TotalStocks: lastLog.TotalStocks,
            SucceededStocks: lastLog.SucceededStocks,
            FailedStocks: lastLog.FailedStocks,
            TriggeredBy: lastLog.TriggeredBy,
            HadErrors: lastLog.ErrorSummary is not null
        );

        return Ok(new ScrapeStatusResponse(
            Status: snapshot.Status.ToString(),
            Live: snapshot.Status is ScrapeRunStatus.NotRunning ? null : MapLive(snapshot),
            LastRun: lastRun
        ));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static LiveProgressInfo MapLive(ScrapeProgressSnapshot s) => new(
        TriggeredBy: s.TriggeredBy,
        TotalStocks: s.TotalStocks,
        ProcessedCount: s.ProcessedCount,
        SucceededCount: s.SucceededCount,
        FailedCount: s.FailedCount,
        PercentComplete: s.PercentComplete,
        CurrentStockTicker: s.CurrentStockTicker,
        StartedAt: s.StartedAt,
        ElapsedSeconds: s.ElapsedSeconds,
        EstimatedSecondsRemaining: s.EstimatedSecondsRemaining
    );
}

// ── Response shapes ───────────────────────────────────────────────────────────

/// <summary>Top-level response from GET /api/scraping/status.</summary>
public sealed record ScrapeStatusResponse(
    /// <summary>One of: NotRunning | Running | Committing | Completed | Failed</summary>
    string Status,
    /// <summary>Populated while a run is Running or Committing; also populated briefly after Completed/Failed.</summary>
    LiveProgressInfo? Live,
    /// <summary>Populated when Status is NotRunning/Completed/Failed. Null if no run has ever been recorded.</summary>
    LastRunSummary? LastRun
);

/// <summary>Live per-stock progress, ready to bind directly to a progress bar.</summary>
public sealed record LiveProgressInfo(
    string? TriggeredBy,
    int TotalStocks,
    int ProcessedCount,
    int SucceededCount,
    int FailedCount,
    /// <summary>0.0–100.0, 1 decimal place. Bind directly to progress bar value/width.</summary>
    double PercentComplete,
    /// <summary>Ticker currently being scraped, e.g. "COMI". Null during commit phase.</summary>
    string? CurrentStockTicker,
    DateTime? StartedAt,
    /// <summary>Pre-computed: (now − StartedAt).TotalSeconds, rounded to 1 decimal.</summary>
    double ElapsedSeconds,
    /// <summary>Null until 5 stocks complete (avoids wild early estimates). Rounded to whole seconds.</summary>
    double? EstimatedSecondsRemaining
);

/// <summary>Summary of the last persisted scrape run, pulled from ScrapeRunLog.</summary>
public sealed record LastRunSummary(
    DateTime RunAt,
    DateTime? FinishedAt,
    int? DurationSeconds,
    int TotalStocks,
    int SucceededStocks,
    int FailedStocks,
    string TriggeredBy,
    bool HadErrors
);
