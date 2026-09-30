using MediatR;
using Meezan.Application.Common;
using Meezan.Application.Common.Interfaces;
using Meezan.Application.Features.Scraping.Services;
using Meezan.Domain.Entities;
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

        // Overlap guard — two concurrent runs would interleave the atomic
        // live-table commit (global DELETE + re-INSERT) and deadlock each other,
        // leaving zero-filled ScrapeRunLog rows behind. Refuse politely instead.
        var activeSnapshot = _progress.GetSnapshot();
        if (activeSnapshot.Status is ScrapeRunStatus.Running or ScrapeRunStatus.Committing)
        {
            var busyMsg = $"Another scrape run ('{activeSnapshot.TriggeredBy}') is already in progress " +
                $"({activeSnapshot.ProcessedCount}/{activeSnapshot.TotalStocks} processed). " +
                "Wait for it to finish before starting a new one.";
            _logger.LogWarning("Scrape run '{TriggeredBy}' rejected: {Reason}", request.TriggeredBy, busyMsg);
            return new RunCombinedScrapeResult(
                Success: false,
                TotalStocks: activeSnapshot.TotalStocks,
                Succeeded: activeSnapshot.SucceededCount,
                Failed: activeSnapshot.FailedCount,
                Duration: TimeSpan.Zero,
                ErrorSummary: busyMsg);
        }

        // Record the run start — the row is FINALISED in the finally block below,
        // so it is ALWAYS updated, even if the run crashes midway.
        var log = await _runLogRepo.CreateAsync(new ScrapeRunLog
        {
            TriggeredBy = request.TriggeredBy,
            StartedAt = startedAt
        }, cancellationToken);

        List<Stock> stocks;
        try
        {
            stocks = await _stockRepository.GetAllWithMarketDataAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Stock-list load failure — fail fast with a persisted log entry
            // instead of leaving a zero-filled run row behind.
            _logger.LogError(ex, "Scrape run '{TriggeredBy}': failed to load stock list.", request.TriggeredBy);
            _progress.SetFailed($"Failed to load stock list: {ex.Message}");
            var loadFinished = DateTime.UtcNow;
            log.FinishedAt = loadFinished;
            log.TotalStocks = 0;
            log.SucceededStocks = 0;
            log.FailedStocks = 0;
            log.ErrorSummary = $"Failed to load stock list: {ex.Message}";
            await _runLogRepo.UpdateAsync(log, CancellationToken.None);
            return new RunCombinedScrapeResult(false, 0, 0, 0, loadFinished - startedAt, log.ErrorSummary);
        }

        int total = stocks.Count;
        int succeeded = 0;
        int failed = 0;
        var errors = new List<string>();
        string? fatalError = null;
        string? errorSummary = null;

        // Informational only: tickers whose Mubasher page shows a historical date.
        // Stale data is STAGED NORMALLY (it is still the latest known-good data) and
        // NEVER triggers any automatic write, flag change, or deactivation — it only
        // surfaces in the operator review checklist (GET /api/scraping/removal-candidates).
        var staleTickers = new List<string>();

        // Initialise the live progress tracker so polling starts immediately.
        _progress.StartRun(request.TriggeredBy, total);

        try
        {
        // ── Phase 1: scrape raw data for all stocks into memory ──────────────
        // No fair-value computation yet — we need ALL stocks' PE/PB values
        // before we can compute sector-peer medians.

        var rawScraped = new List<RawStockScrape>(total);

        foreach (var stock in stocks)
        {
            if (cancellationToken.IsCancellationRequested) break;

            _progress.SetCurrentTicker(stock.Ticker);

            bool stockSucceeded = false;
            try
            {
                var raw = await ScrapeRawAsync(stock, request.Bucket, cancellationToken);

                // Check 1: 404 Not Found on Mubasher — nothing to stage.
                // Count as failed for this run; the stock's existing data is left
                // EXACTLY as-is (no deactivation, no flag change). It surfaces in
                // the operator review checklist as NotFoundOnSource.
                if (raw.IsNotFound)
                {
                    _logger.LogWarning(
                        "Ticker {Ticker} returned 404 Not Found on Mubasher — leaving existing data untouched.",
                        stock.Ticker);
                    failed++;
                    errors.Add($"{stock.Ticker}: 404 Not Found on Mubasher");
                    continue;
                }

                // No usable payload at all (every retry failed) — failed for this
                // run, no state touched.
                if (raw.MarketData is null && raw.SupportResistance is null)
                {
                    _logger.LogWarning(
                        "No data returned for {Ticker} — leaving existing data untouched.",
                        stock.Ticker);
                    failed++;
                    errors.Add($"{stock.Ticker}: no market data returned");
                    continue;
                }

                // Check 2: Mubasher last-update shows a historical date.
                // The data is STAGED NORMALLY (a date-stamped close is still the
                // latest known-good data) and counts as SUCCEEDED. Recorded here
                // for the review checklist only — never acted upon automatically.
                if (raw.IsStale)
                {
                    staleTickers.Add($"{stock.Ticker} ({raw.MarketData?.SourceLastUpdateText ?? "?"})");
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

        // ── No automatic state changes, ever ─────────────────────────────────
        // Stale / 404 / missing-data stocks are NEVER deactivated, flagged, or
        // otherwise written by the scraper. They only appear in the operator
        // review checklist (GET /api/scraping/removal-candidates) where a human
        // explicitly confirms deactivation or triggers a selective refresh.

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
                // Record the failure and fall through to the finally block so the
                // ScrapeRunLog row is still finalised (Succeeded = 0, Failed = total).
                _logger.LogError(ex,
                    "Batch commit FAILED — live tables have NOT been modified. " +
                    "Scrape run will be logged as failed.");

                fatalError = $"Commit failed: {ex.Message}";
                succeeded = 0;
                failed = total;
                _progress.SetFailed(ex.Message);
            }
        }
        else
        {
            _progress.SetFailed("No stocks were successfully scraped.");
            _logger.LogWarning("No stocks were successfully scraped — skipping commit. Live tables unchanged.");
        }
        } // ── end outer try ──
        catch (Exception fatalEx)
        {
            // Any unexpected crash (fair-value calc, sector medians, commit plumbing…).
            // The finally block below still persists the ScrapeRunLog row.
            fatalError = fatalEx.Message;
            _logger.LogError(fatalEx,
                "Scrape run '{TriggeredBy}' crashed unexpectedly — persisting partial results.",
                request.TriggeredBy);
            _progress.SetFailed(fatalEx.Message);
        }
        finally
        {
            string? staleNote = staleTickers.Count > 0
                ? $"Stale Mubasher date staged as-is for {staleTickers.Count} stock(s) " +
                  "(review via removal-candidates checklist, no auto-action taken): " +
                  string.Join(", ", staleTickers.Take(20)) + (staleTickers.Count > 20 ? "…" : "")
                : null;
            errorSummary = errors.Count > 0 ? string.Join("; ", errors) : null;
            if (staleNote is not null)
                errorSummary = staleNote + (errorSummary is not null ? "; " + errorSummary : "");
            if (fatalError is not null)
                errorSummary = fatalError + (errorSummary is not null ? $"; Scrape errors: {errorSummary}" : "");

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
        }

        return new RunCombinedScrapeResult(
            Success: fatalError is null && failed == 0,
            TotalStocks: total,
            Succeeded: succeeded,
            Failed: failed,
            Duration: DateTime.UtcNow - startedAt,
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
                // FIX: For staleness detection, always use the stock's EXISTING SourceLastUpdateText
                // from the DB (not the freshly-scraped value). If the fresh scrape itself shows a
                // stale date, that could be a transient issue. Our policy is to only deactivate
                // a stock when our own stored record is also stale — i.e. the stock has been
                // consistently absent from Mubasher's live feed for an extended period.
                ? (stock.MarketData?.SourceLastUpdateText ?? md.SourceLastUpdateText)
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
        // Every stock with scraped market data gets a fair-value row — including
        // "Unavailable" ones (FairValue null, 0 methods) when no valuation input exists.
        // Skipping these used to leave no row at all, which the UI read as a fake
        // "قريبة من العادلة — 0.00" verdict. 404 stocks (MarketData null) get no row.
        if (raw.MarketData is not null)
        {
            var inputValues = new FairValueCalculator.ScrapedMarketDataValues(
                Eps: values.Eps,
                PeRatio: values.PeRatio,
                BookValue: values.BookValue,
                PbRatio: values.PbRatio
            );

            // Primary fair value: Graham only
            var grahamResult = FairValueCalculator.ComputeGrahamOnly(inputValues, values.ClosingPrice);

            // Individual methods for detail display (4 methods, no aggregation)
            var individualMethods = FairValueCalculator.ComputeIndividualMethods(
                inputValues,
                sectorMedians,
                raw.Stock.SectorId);

            // Persist Graham result as primary fair value
            fvEntity = new StockFairValue
            {
                StockId = raw.Stock.Id,
                FairValue = grahamResult.FairValue,
                PriceComparison = grahamResult.Comparison,
                FairValueDiff = grahamResult.DiffAbs,
                FairValueDiffPct = grahamResult.DiffPct,
                MethodsUsedCount = grahamResult.MethodsUsedCount,
                MethodsExcludedCount = grahamResult.MethodsExcludedCount,
                Confidence = grahamResult.Confidence,
                ComputedAt = DateTime.UtcNow
            };

            fvMethods = individualMethods.Select(m => new StockFairValueMethod
            {
                MethodName = m.Name,
                EstimatedValue = m.Value,
                IsOutlier = m.IsOutlier
            }).ToList();
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
