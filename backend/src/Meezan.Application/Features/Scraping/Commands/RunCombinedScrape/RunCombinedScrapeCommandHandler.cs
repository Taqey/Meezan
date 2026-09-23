using MediatR;
using Meezan.Application.Common;
using Meezan.Application.Common.Interfaces;
using Meezan.Application.Features.Scraping.Services;
using Meezan.Domain.Entities;
using Meezan.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Meezan.Application.Features.Scraping.Commands.RunCombinedScrape;

public class RunCombinedScrapeCommandHandler
    : IRequestHandler<RunCombinedScrapeCommand, RunCombinedScrapeResult>
{
    private readonly IStockRepository _stockRepository;
    private readonly IStockScraperClient _scraperClient;
    private readonly IScrapeBatchCommitter _batchCommitter;
    private readonly IScrapeRunLogRepository _runLogRepo;
    private readonly IScrapeProgressTracker _progress;
    private readonly ILogger<RunCombinedScrapeCommandHandler> _logger;

    public RunCombinedScrapeCommandHandler(
        IStockRepository stockRepository,
        IStockScraperClient scraperClient,
        IScrapeBatchCommitter batchCommitter,
        IScrapeRunLogRepository runLogRepo,
        IScrapeProgressTracker progress,
        ILogger<RunCombinedScrapeCommandHandler> logger)
    {
        _stockRepository = stockRepository;
        _scraperClient = scraperClient;
        _batchCommitter = batchCommitter;
        _runLogRepo = runLogRepo;
        _progress = progress;
        _logger = logger;
    }

    public async Task<RunCombinedScrapeResult> Handle(
        RunCombinedScrapeCommand request,
        CancellationToken cancellationToken)
    {
        var startedAt = DateTime.UtcNow;

        // Record the run start — we update it once everything is done.
        var log = await _runLogRepo.CreateAsync(new ScrapeRunLog
        {
            TriggeredBy = request.TriggeredBy,
            StartedAt = startedAt
        }, cancellationToken);

        var stocks = await _stockRepository.GetAllWithMarketDataAsync(cancellationToken);
        int total = stocks.Count;
        int succeeded = 0;
        int failed = 0;
        var errors = new List<string>();

        // Tracks stocks whose market data came back null — flagged as IncompleteNoData.
        var noDataStocks = new List<Stock>();

        // Mass-failure safeguard: if more than this fraction of stocks fail, skip auto-flagging
        // as IncompleteNoData. A near-100% failure rate is evidence of a systemic infra/network
        // failure (e.g. DB commit bug, scraper misconfiguration), NOT evidence of 259 stocks
        // being delisted. Flagging stocks under such conditions would incorrectly hide valid stocks
        // from the frontend. This threshold is checked AFTER Phase 1 completes.
        const double MassFailureThreshold = 0.20; // suppress flagging if >20% of stocks fail

        // Mass-deletion safeguard: if more than this fraction of stocks are flagged for deletion
        // (404 or stale date on Mubasher), abort deletions for this run to avoid mass data loss.
        const double MassDeletionThreshold = 0.10; // suppress deletion if >10% of stocks flagged
        var pendingDeletions = new List<(Stock Stock, string Reason)>();

        // Initialise the live progress tracker so polling starts immediately.
        _progress.StartRun(request.TriggeredBy, total);

        // ── Phase 1: scrape raw data for all stocks into memory ──────────────
        // No fair-value computation yet — we need ALL stocks' PE/PB values
        // before we can compute sector-peer medians.

        var rawScraped = new List<RawStockScrape>(total);
        // Temporarily collect pending IncompleteNoData flags — applied only if failure rate is low.
        var pendingNoDataFlags = new List<Stock>();

        foreach (var stock in stocks)
        {
            if (cancellationToken.IsCancellationRequested) break;

            _progress.SetCurrentTicker(stock.Ticker);

            bool stockSucceeded = false;
            try
            {
                var raw = await ScrapeRawAsync(stock, request.Bucket, cancellationToken);

                // Check 1: 404 Not Found on Mubasher -> queue for hard deletion
                if (raw.IsNotFound)
                {
                    _logger.LogWarning(
                        "Ticker {Ticker} returned 404 Not Found on Mubasher — queuing for permanent deletion.",
                        stock.Ticker);
                    pendingDeletions.Add((stock, "404 Not Found on Mubasher"));
                    failed++;
                    errors.Add($"{stock.Ticker}: 404 Not Found on Mubasher");
                    continue;
                }

                // Check 2: Mubasher last-update has a date instead of time -> queue for hard deletion
                if (raw.IsStale)
                {
                    _logger.LogWarning(
                        "Ticker {Ticker} has stale last update date '{DateText}' on Mubasher — queuing for permanent deletion.",
                        stock.Ticker, raw.MarketData?.SourceLastUpdateText);
                    pendingDeletions.Add((stock, $"Stale date on Mubasher: {raw.MarketData?.SourceLastUpdateText}"));
                    failed++;
                    errors.Add($"{stock.Ticker}: Stale date on Mubasher");
                    continue;
                }

                // If market data came back null, this ticker has no Mubasher page.
                // Tentatively flag as IncompleteNoData — confirmed below after Phase 1 if
                // the failure rate is below the mass-failure threshold.
                if (raw.MarketData is null)
                {
                    if (stock.DataStatus == StockDataStatus.Active)
                    {
                        _logger.LogWarning(
                            "No market data returned for {Ticker} — tentatively marking as IncompleteNoData (pending failure-rate check).",
                            stock.Ticker);
                        pendingNoDataFlags.Add(stock);
                    }
                }
                else if (stock.DataStatus == StockDataStatus.IncompleteNoData)
                {
                    // Data came back — restore to Active unconditionally (safe regardless of failure rate).
                    _logger.LogInformation(
                        "Market data returned for previously-incomplete {Ticker} — restoring to Active.",
                        stock.Ticker);
                    stock.DataStatus = StockDataStatus.Active;
                    stock.UpdatedAt = DateTime.UtcNow;
                    noDataStocks.Add(stock);
                }

                rawScraped.Add(raw);
                succeeded++;
                stockSucceeded = true;
            }
            catch (Exception ex)
            {
                failed++;
                var msg = $"{stock.Ticker}: {ex.Message}";
                _logger.LogWarning("Scrape failed for {Ticker}: {Error}", stock.Ticker, ex.Message);
                if (errors.Count < 20) errors.Add(msg);
            }
            finally
            {
                _progress.RecordStockCompleted(stock.Ticker, stockSucceeded);
            }
        }

        // ── Mass-deletion safeguard: permanently delete stale / 404 stocks ──
        double deletionRate = total > 0 ? (double)pendingDeletions.Count / total : 0;
        bool isMassDeletion = deletionRate > MassDeletionThreshold;

        if (isMassDeletion)
        {
            _logger.LogError(
                "MASS DELETION SAFEGUARD TRIGGERED: {Count}/{Total} stocks flagged for deletion ({Rate:P0}, threshold={Threshold:P0}). " +
                "Aborting all auto-deletions for this run — likely a scraping or network issue.",
                pendingDeletions.Count, total, deletionRate, MassDeletionThreshold);
        }
        else if (pendingDeletions.Count > 0)
        {
            _logger.LogInformation(
                "Proceeding with permanent auto-deletion of {Count} stale/404 stock(s).",
                pendingDeletions.Count);

            foreach (var (staleStock, reason) in pendingDeletions)
            {
                try
                {
                    await _stockRepository.DeletePermanentlyAsync(staleStock.Id, CancellationToken.None);
                    _logger.LogInformation(
                        "Permanently deleted stock {Ticker} (ID: {Id}). Reason: {Reason}",
                        staleStock.Ticker, staleStock.Id, reason);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to permanently delete stock {Ticker} (ID: {Id}).",
                        staleStock.Ticker, staleStock.Id);
                }
            }
        }

        // ── Mass-failure check: decide whether to apply pending IncompleteNoData flags ──
        double failureRate = total > 0 ? (double)failed / total : 0;
        bool isMassFailure = failureRate > MassFailureThreshold;

        if (isMassFailure)
        {
            _logger.LogError(
                "MASS FAILURE DETECTED: {Failed}/{Total} stocks failed ({Rate:P0} failure rate, threshold={Threshold:P0}). " +
                "Suppressing all IncompleteNoData auto-flagging for this run — " +
                "a systemic failure is not evidence of stocks being delisted.",
                failed, total, failureRate, MassFailureThreshold);
            // pendingNoDataFlags are discarded — do NOT mutate stock.DataStatus.
        }
        else
        {
            // Failure rate is within acceptable range — apply the pending IncompleteNoData flags.
            foreach (var stock in pendingNoDataFlags)
            {
                stock.DataStatus = StockDataStatus.IncompleteNoData;
                stock.UpdatedAt = DateTime.UtcNow;
                noDataStocks.Add(stock);
                _logger.LogWarning(
                    "Confirmed IncompleteNoData for {Ticker} (failure rate {Rate:P0} within threshold).",
                    stock.Ticker, failureRate);
            }
        }

        // ── Phase 2: compute sector-peer medians from the full scraped batch ──

        var peerSource = request.Bucket == ScrapeDataBucket.LiveDaily
            ? rawScraped.Where(r => r.MarketData is not null)
            : rawScraped;

        var peerData = peerSource
            .Where(r => r.Stock.SectorId.HasValue)
            .Select(r =>
            {
                var values = GetValuationSource(r);
                return new FairValueCalculator.SectorPeerData(
                    StockId: r.Stock.Id,
                    SectorId: r.Stock.SectorId,
                    PeRatio: values.PeRatio,
                    PbRatio: values.PbRatio);
            })
            .ToList();

        var sectorMedians = FairValueCalculator.ComputeSectorMedians(peerData);

        _logger.LogInformation(
            "Sector medians computed for {Count} sectors from {Peers} scraped stocks.",
            sectorMedians.Count, peerData.Count);

        // Log sector-median detail at debug level for verification
        foreach (var (sid, med) in sectorMedians)
        {
            var medPe = ComputeListMedian(med.PeValues);
            var medPb = ComputeListMedian(med.PbValues);
            _logger.LogDebug(
                "Sector {SectorId}: {PeCount} PE peers (median={MedianPE:F2}), " +
                "{PbCount} PB peers (median={MedianPB:F2})",
                sid, med.PeValues.Count, medPe, med.PbValues.Count, medPb);
        }

        // ── Phase 3: compute fair values and assemble final snapshots ─────────

        var snapshots = new List<StockScrapedSnapshot>(rawScraped.Count);

        foreach (var raw in rawScraped)
        {
            sectorMedians.TryGetValue(raw.Stock.SectorId ?? -1, out var medians);
            var snapshot = BuildSnapshot(raw, medians);
            snapshots.Add(snapshot);
        }

        // ── Phase 4: atomic commit ── signal Committing status, then write ────

        _progress.SetCommitting();
        string? massDeletionNote = isMassDeletion
            ? $"Mass-deletion safeguard triggered: {pendingDeletions.Count}/{total} stocks ({deletionRate:P0}) flagged — auto-deletion aborted."
            : null;
        string? massFailureNote = isMassFailure
            ? $"Mass-failure safeguard triggered: {failed}/{total} failures ({failureRate:P0}) — IncompleteNoData flagging suppressed."
            : null;
        string? errorSummary = errors.Count > 0 ? string.Join("; ", errors) : null;
        if (massDeletionNote is not null)
            errorSummary = massDeletionNote + (errorSummary is not null ? "; " + errorSummary : "");
        if (massFailureNote is not null)
            errorSummary = massFailureNote + (errorSummary is not null ? "; " + errorSummary : "");

        // Persist any DataStatus changes (IncompleteNoData / Active restorations).
        // noDataStocks only contains safe changes at this point — Active restorations
        // are always included; IncompleteNoData flags are already suppressed above if
        // the mass-failure threshold was exceeded.
        if (noDataStocks.Count > 0)
        {
            try
            {
                foreach (var s in noDataStocks)
                    await _stockRepository.UpdateAsync(s, CancellationToken.None);
                await _stockRepository.SaveChangesAsync(CancellationToken.None);

                _logger.LogInformation(
                    "Persisted DataStatus changes for {Count} stock(s).", noDataStocks.Count);
            }
            catch (Exception ex)
            {
                // Non-fatal — log and continue; we don't want to block the market-data commit.
                _logger.LogError(ex, "Failed to persist DataStatus changes — continuing with market-data commit.");
            }
        }

        if (snapshots.Count > 0)
        {
            try
            {
                if (request.Bucket == ScrapeDataBucket.LiveDaily)
                {
                    await _batchCommitter.CommitLiveAsync(snapshots, cancellationToken);
                }
                else
                {
                    await _batchCommitter.CommitSlowAsync(snapshots, cancellationToken);
                }
                _logger.LogInformation(
                    "Batch commit succeeded. {Count} stocks written atomically.", snapshots.Count);
                _progress.SetCompleted();
            }
            catch (Exception ex)
            {
                // The transaction was rolled back — live tables are unchanged.
                _logger.LogError(ex,
                    "Batch commit FAILED — live tables have NOT been modified. " +
                    "Scrape run will be logged as failed.");

                _progress.SetFailed(ex.Message);

                var finished = DateTime.UtcNow;
                log.FinishedAt = finished;
                log.TotalStocks = total;
                log.SucceededStocks = 0;
                log.FailedStocks = total;
                log.ErrorSummary = $"Commit failed: {ex.Message}" +
                    (errorSummary is not null ? $"; Scrape errors: {errorSummary}" : "");
                await _runLogRepo.UpdateAsync(log, CancellationToken.None);

                return new RunCombinedScrapeResult(
                    Success: false,
                    TotalStocks: total,
                    Succeeded: 0,
                    Failed: total,
                    Duration: finished - startedAt,
                    ErrorSummary: log.ErrorSummary
                );
            }
        }
        else
        {
            _progress.SetFailed("No stocks were successfully scraped.");
            _logger.LogWarning("No stocks were successfully scraped — skipping commit. Live tables unchanged.");
        }

        var finishedAt = DateTime.UtcNow;
        log.FinishedAt = finishedAt;
        log.TotalStocks = total;
        log.SucceededStocks = succeeded;
        log.FailedStocks = failed;
        log.ErrorSummary = errorSummary;
        await _runLogRepo.UpdateAsync(log, CancellationToken.None); // use None so a cancel doesn't skip the log

        _logger.LogInformation(
            "Scrape run complete. Total: {Total}, Succeeded: {Succeeded}, Failed: {Failed}, Duration: {Duration}s",
            total, succeeded, failed, (int)(finishedAt - startedAt).TotalSeconds);

        return new RunCombinedScrapeResult(
            Success: failed == 0,
            TotalStocks: total,
            Succeeded: succeeded,
            Failed: failed,
            Duration: finishedAt - startedAt,
            ErrorSummary: errorSummary
        );
    }

    // ── Phase 1 helper: scrape raw data for one stock, no fair-value calc ────

    private async Task<RawStockScrape> ScrapeRawAsync(
        Stock stock,
        ScrapeDataBucket bucket,
        CancellationToken ct)
    {
        StockMarketData? mdEntity = null;
        StockSupportResistance? srEntity = null;

        // Market data
        var md = await _scraperClient.ScrapeMarketDataAsync(stock.Ticker, ct);
        bool isNotFound = md?.IsNotFound == true;
        bool isStale = false;

        if (md is not null && !isNotFound)
        {
            var updateText = bucket == ScrapeDataBucket.LiveDaily
                ? md.SourceLastUpdateText
                : (stock.MarketData?.SourceLastUpdateText ?? md.SourceLastUpdateText);

            if (MubasherStaleDetector.IsStale(updateText))
            {
                isStale = true;
            }

            var fetchedAt = DateTime.UtcNow;
            if (bucket == ScrapeDataBucket.LiveDaily)
            {
                var existing = stock.MarketData;
                mdEntity = new StockMarketData
                {
                    StockId = stock.Id,
                    NominalValue = existing?.NominalValue,
                    MarketValue = existing?.MarketValue,
                    BookValue = existing?.BookValue,
                    PbRatio = existing?.PbRatio,
                    Eps = existing?.Eps,
                    PeRatio = existing?.PeRatio,
                    Currency = existing?.Currency,
                    High = md.High,
                    Low = md.Low,
                    Open = md.Open,
                    ClosingPrice = md.ClosingPrice,
                    SourceLastUpdateText = md.SourceLastUpdateText,
                    FetchedAt = fetchedAt,
                    SlowDataFetchedAt = existing?.SlowDataFetchedAt
                };
            }
            else
            {
                var latestClose = stock.MarketData?.ClosingPrice;
                var peRatio = CalculateRatio(latestClose, md.Eps);
                var pbRatio = CalculateRatio(latestClose, md.BookValue);

                mdEntity = new StockMarketData
                {
                    StockId = stock.Id,
                    NominalValue = md.NominalValue,
                    MarketValue = md.MarketValue,
                    BookValue = md.BookValue,
                    PbRatio = pbRatio,
                    Eps = md.Eps,
                    PeRatio = peRatio,
                    Currency = md.Currency,
                    High = stock.MarketData?.High,
                    Low = stock.MarketData?.Low,
                    Open = stock.MarketData?.Open,
                    ClosingPrice = latestClose,
                    SourceLastUpdateText = stock.MarketData?.SourceLastUpdateText,
                    FetchedAt = stock.MarketData?.FetchedAt ?? fetchedAt,
                    SlowDataFetchedAt = fetchedAt
                };
            }
        }

        if (bucket == ScrapeDataBucket.LiveDaily)
        {
            // Support & resistance
            var sr = await _scraperClient.ScrapeSupportResistanceAsync(stock.Ticker, ct);
            if (sr is not null)
            {
                srEntity = new StockSupportResistance
                {
                    StockId = stock.Id,
                    LastPrice = sr.LastPrice,
                    ChangePct = sr.ChangePct,
                    Pivot = sr.Pivot,
                    R1 = sr.R1,
                    R2 = sr.R2,
                    S1 = sr.S1,
                    S2 = sr.S2,
                    FetchedAt = DateTime.UtcNow
                };
            }
        }

        return new RawStockScrape(stock, mdEntity, srEntity, isNotFound, isStale);
    }

    // ── Phase 3 helper: build the final snapshot with fair-value applied ──────

    private StockScrapedSnapshot BuildSnapshot(
        RawStockScrape raw,
        FairValueCalculator.SectorMedians? sectorMedians)
    {
        StockFairValue? fvEntity = null;
        List<StockFairValueMethod>? fvMethods = null;

        var values = GetValuationSource(raw);
        if (values.HasAnyValue)
        {
            var inputValues = new FairValueCalculator.ScrapedMarketDataValues(
                Eps: values.Eps,
                PeRatio: values.PeRatio,
                BookValue: values.BookValue,
                PbRatio: values.PbRatio
            );

            var methodEstimates = FairValueCalculator.BuildMethodEstimates(
                inputValues,
                sectorMedians,
                raw.Stock.SectorId);

            var result = FairValueCalculator.Compute(methodEstimates, values.ClosingPrice);

            if (result.FairValue.HasValue)
            {
                fvEntity = new StockFairValue
                {
                    StockId = raw.Stock.Id,
                    FairValue = result.FairValue,
                    PriceComparison = result.Comparison,
                    FairValueDiff = result.DiffAbs,
                    FairValueDiffPct = result.DiffPct,
                    MethodsUsedCount = result.MethodsUsedCount,
                    MethodsExcludedCount = result.MethodsExcludedCount,
                    Confidence = result.Confidence,
                    ComputedAt = DateTime.UtcNow
                };

                fvMethods = result.Methods.Select(m => new StockFairValueMethod
                {
                    MethodName = m.Name,
                    EstimatedValue = m.Value,
                    IsOutlier = m.IsOutlier
                }).ToList();
            }
        }

        return new StockScrapedSnapshot(
            StockId: raw.Stock.Id,
            Ticker: raw.Stock.Ticker,
            MarketData: raw.MarketData,
            FairValue: fvEntity,
            FairValueMethods: fvMethods,
            SupportResistance: raw.SupportResistance
        );
    }

    // ── Utility: median of a list ─────────────────────────────────────────────

    private static double? ComputeListMedian(IReadOnlyList<double> values)
    {
        if (values.Count == 0) return null;
        var sorted = values.OrderBy(v => v).ToList();
        int mid = sorted.Count / 2;
        return sorted.Count % 2 == 1
            ? sorted[mid]
            : (sorted[mid - 1] + sorted[mid]) / 2.0;
    }

    private static decimal? CalculateRatio(decimal? numerator, decimal? denominator)
    {
        if (!numerator.HasValue || !denominator.HasValue || denominator.Value <= 0)
        {
            return null;
        }

        return Math.Round(numerator.Value / denominator.Value, 4, MidpointRounding.AwayFromZero);
    }

    private static ValuationValues GetValuationSource(RawStockScrape raw)
    {
        var md = raw.MarketData;
        var existing = raw.Stock.MarketData;
        return new ValuationValues(
            Eps: md?.Eps ?? existing?.Eps,
            PeRatio: md?.PeRatio ?? existing?.PeRatio,
            BookValue: md?.BookValue ?? existing?.BookValue,
            PbRatio: md?.PbRatio ?? existing?.PbRatio,
            ClosingPrice: md?.ClosingPrice ?? existing?.ClosingPrice);
    }

    // ── Private intermediate container ────────────────────────────────────────

    private sealed record RawStockScrape(
        Stock Stock,
        StockMarketData? MarketData,
        StockSupportResistance? SupportResistance,
        bool IsNotFound = false,
        bool IsStale = false
    );

    private sealed record ValuationValues(
        decimal? Eps,
        decimal? PeRatio,
        decimal? BookValue,
        decimal? PbRatio,
        decimal? ClosingPrice)
    {
        public bool HasAnyValue =>
            Eps.HasValue ||
            PeRatio.HasValue ||
            BookValue.HasValue ||
            PbRatio.HasValue ||
            ClosingPrice.HasValue;
    }
}
