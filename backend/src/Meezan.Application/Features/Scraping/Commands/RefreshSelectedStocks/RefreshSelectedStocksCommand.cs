using MediatR;
using Meezan.Application.Common;
using Meezan.Application.Common.Interfaces;
using Meezan.Application.Features.Scraping.Services;
using Meezan.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Meezan.Application.Features.Scraping.Commands.RefreshSelectedStocks;

/// <summary>
/// Operator-initiated re-scrape of an explicit list of tickers (e.g. checked off
/// in the removal-candidates review checklist). Commits ONLY those stocks via a
/// scoped upsert — every other stock's data is untouched.
/// </summary>
public record RefreshSelectedStocksCommand(
    List<string> Tickers,
    string TriggeredBy = "Manual-RefreshSelected") : IRequest<RefreshSelectedStocksResult>;

public record SelectedStockRefreshOutcome(
    string Ticker,
    bool Success,
    string? Message);

public record RefreshSelectedStocksResult(
    bool Success,
    int TotalRequested,
    int TotalFound,
    int Succeeded,
    int Failed,
    TimeSpan Duration,
    string? ErrorSummary,
    List<SelectedStockRefreshOutcome> Outcomes);

public class RefreshSelectedStocksCommandHandler
    : IRequestHandler<RefreshSelectedStocksCommand, RefreshSelectedStocksResult>
{
    private const int MaxBatchSize = 50;

    private readonly IStockRepository _stockRepository;
    private readonly IStockScraperClient _scraperClient;
    private readonly IScrapeBatchCommitter _batchCommitter;
    private readonly IScrapeRunLogRepository _runLogRepo;
    private readonly IScrapeProgressTracker _progress;
    private readonly ILogger<RefreshSelectedStocksCommandHandler> _logger;

    public RefreshSelectedStocksCommandHandler(
        IStockRepository stockRepository,
        IStockScraperClient scraperClient,
        IScrapeBatchCommitter batchCommitter,
        IScrapeRunLogRepository runLogRepo,
        IScrapeProgressTracker progress,
        ILogger<RefreshSelectedStocksCommandHandler> logger)
    {
        _stockRepository = stockRepository;
        _scraperClient = scraperClient;
        _batchCommitter = batchCommitter;
        _runLogRepo = runLogRepo;
        _progress = progress;
        _logger = logger;
    }

    public async Task<RefreshSelectedStocksResult> Handle(
        RefreshSelectedStocksCommand request,
        CancellationToken cancellationToken)
    {
        var startedAt = DateTime.UtcNow;

        var tickers = request.Tickers
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim().ToUpperInvariant())
            .Distinct()
            .Take(MaxBatchSize)
            .ToList();

        if (tickers.Count == 0)
        {
            return new RefreshSelectedStocksResult(
                false, 0, 0, 0, 0, TimeSpan.Zero,
                "No tickers supplied.", new List<SelectedStockRefreshOutcome>());
        }

        // Never interleave with a full run's global-delete commit.
        var active = _progress.GetSnapshot();
        if (active.Status is ScrapeRunStatus.Running or ScrapeRunStatus.Committing)
        {
            var busy = $"A full scrape run ('{active.TriggeredBy}') is currently in progress. " +
                "Wait for it to finish before refreshing selected stocks.";
            _logger.LogWarning("RefreshSelectedStocks rejected: {Reason}", busy);
            return new RefreshSelectedStocksResult(
                false, tickers.Count, 0, 0, tickers.Count, TimeSpan.Zero,
                busy, tickers.Select(t => new SelectedStockRefreshOutcome(t, false, busy)).ToList());
        }

        var log = await _runLogRepo.CreateAsync(new ScrapeRunLog
        {
            TriggeredBy = $"{request.TriggeredBy} [{string.Join(",", tickers)}]",
            StartedAt = startedAt
        }, cancellationToken);

        int succeeded = 0;
        int failed = 0;
        var errors = new List<string>();
        var staleInfo = new List<string>();
        var outcomes = new List<SelectedStockRefreshOutcome>();
        var snapshots = new List<StockScrapedSnapshot>();
        var refreshedStocks = new List<Stock>();

        try
        {
            var stocks = await _stockRepository.GetByTickersIncludingInactiveAsync(tickers, cancellationToken);
            var byTicker = stocks.ToDictionary(s => s.Ticker.ToUpperInvariant(), s => s);

            foreach (var missing in tickers.Where(t => !byTicker.ContainsKey(t)))
            {
                failed++;
                errors.Add($"{missing}: not found in database");
                outcomes.Add(new SelectedStockRefreshOutcome(missing, false, "Not found in database"));
            }

            // Sector-peer medians from currently-stored active stocks (read-only).
            var peers = await _stockRepository.GetAllWithMarketDataAsync(cancellationToken);
            var peerData = peers
                .Where(s => s.SectorId.HasValue && (s.MarketData?.PeRatio.HasValue == true || s.MarketData?.PbRatio.HasValue == true))
                .Select(s => new FairValueCalculator.SectorPeerData(
                    s.Id, s.SectorId, s.MarketData!.PeRatio, s.MarketData.PbRatio))
                .ToList();
            var sectorMedians = FairValueCalculator.ComputeSectorMedians(peerData);

            foreach (var ticker in tickers.Where(t => byTicker.ContainsKey(t)))
            {
                if (cancellationToken.IsCancellationRequested) break;
                var stock = byTicker[ticker];

                try
                {
                    var md = await _scraperClient.ScrapeMarketDataAsync(stock.Ticker, cancellationToken);
                    if (md?.IsNotFound == true)
                    {
                        failed++;
                        errors.Add($"{ticker}: 404 Not Found on Mubasher");
                        outcomes.Add(new SelectedStockRefreshOutcome(ticker, false, "404 Not Found on Mubasher — data left untouched"));
                        continue;
                    }

                    StockMarketData? mdEntity = null;
                    if (md is not null)
                    {
                        var existing = stock.MarketData;
                        var fetchedAt = DateTime.UtcNow;
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

                        if (MubasherStaleDetector.IsStale(md.SourceLastUpdateText))
                            staleInfo.Add($"{ticker} ({md.SourceLastUpdateText})");
                    }

                    StockSupportResistance? srEntity = null;
                    var sr = await _scraperClient.ScrapeSupportResistanceAsync(stock.Ticker, cancellationToken);
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

                    if (mdEntity is null && srEntity is null)
                    {
                        failed++;
                        errors.Add($"{ticker}: no data returned");
                        outcomes.Add(new SelectedStockRefreshOutcome(ticker, false, "No data returned — left untouched"));
                        continue;
                    }

                    // Fair value from fresh close + stored slow fundamentals.
                    StockFairValue? fvEntity = null;
                    List<StockFairValueMethod>? fvMethods = null;
                    var eps = mdEntity?.Eps ?? stock.MarketData?.Eps;
                    var pe = mdEntity?.PeRatio ?? stock.MarketData?.PeRatio;
                    var bv = mdEntity?.BookValue ?? stock.MarketData?.BookValue;
                    var pb = mdEntity?.PbRatio ?? stock.MarketData?.PbRatio;
                    var close = mdEntity?.ClosingPrice ?? stock.MarketData?.ClosingPrice;
                    // Attempt a fair value whenever the stock has market data — with no
                    // valuation input at all this yields an explicit Unavailable row
                    // (FairValue null, 0 methods) instead of no row.
                    if (mdEntity is not null || stock.MarketData is not null)
                    {
                        sectorMedians.TryGetValue(stock.SectorId ?? -1, out var medians);
                        var estimates = FairValueCalculator.BuildMethodEstimates(
                            new FairValueCalculator.ScrapedMarketDataValues(eps, pe, bv, pb),
                            medians, stock.SectorId);
                        var result = FairValueCalculator.Compute(estimates, close);
                        // Persist Unavailable results too (FairValue null) so the frontend
                        // never has to infer a verdict from a missing row.
                        fvEntity = new StockFairValue
                        {
                            StockId = stock.Id,
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

                    snapshots.Add(new StockScrapedSnapshot(stock.Id, stock.Ticker, mdEntity, fvEntity, fvMethods, srEntity));
                    refreshedStocks.Add(stock);
                    succeeded++;
                    outcomes.Add(new SelectedStockRefreshOutcome(ticker, true, "Re-scraped and committed"));
                }
                catch (Exception ex)
                {
                    failed++;
                    var msg = $"{ticker}: {ex.Message}";
                    _logger.LogWarning("Selective refresh failed for {Ticker}: {Error}", ticker, ex.Message);
                    if (errors.Count < 20) errors.Add(msg);
                    outcomes.Add(new SelectedStockRefreshOutcome(ticker, false, ex.Message));
                }
            }

            if (snapshots.Count > 0)
            {
                try
                {
                    await _batchCommitter.CommitSelectedLiveAsync(snapshots, cancellationToken);
                    _logger.LogInformation(
                        "Selective refresh commit succeeded for {Count} stock(s).", snapshots.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Selective refresh commit FAILED — no rows modified.");
                    errors.Insert(0, $"Commit failed: {ex.Message}");
                    foreach (var o in outcomes.Where(o => o.Success).ToList())
                    {
                        outcomes[outcomes.IndexOf(o)] = o with { Success = false, Message = $"Commit failed: {ex.Message}" };
                    }
                    succeeded = 0;
                    failed = tickers.Count;
                    refreshedStocks.Clear();
                    snapshots.Clear();
                }
            }

            // Explicit operator-initiated refresh succeeded → restore to Active.
            // This is the ONLY place a DataStatus write happens, and only for the
            // tickers the operator explicitly selected. Single SQL statement — the
            // entity graph held by this handler is never marked Modified.
            if (snapshots.Count > 0)
            {
                await _stockRepository.SetActiveDataStatusAsync(
                    refreshedStocks.Select(s => s.Id), CancellationToken.None);
            }
        }
        catch (Exception fatalEx)
        {
            _logger.LogError(fatalEx, "RefreshSelectedStocks crashed unexpectedly.");
            errors.Insert(0, fatalEx.Message);
            succeeded = 0;
        }

        string? errorSummary = errors.Count > 0 ? string.Join("; ", errors) : null;
        if (staleInfo.Count > 0)
        {
            var note = $"Staged despite stale Mubasher date: {string.Join(", ", staleInfo)}";
            errorSummary = note + (errorSummary is not null ? "; " + errorSummary : "");
        }

        var finishedAt = DateTime.UtcNow;
        log.FinishedAt = finishedAt;
        log.TotalStocks = tickers.Count;
        log.SucceededStocks = succeeded;
        log.FailedStocks = failed;
        log.ErrorSummary = errorSummary;
        await _runLogRepo.UpdateAsync(log, CancellationToken.None);

        return new RefreshSelectedStocksResult(
            Success: errors.Count == 0 && failed == 0,
            TotalRequested: tickers.Count,
            TotalFound: tickers.Count - tickers.Count(t => outcomes.Any(o => o.Ticker == t && o.Message == "Not found in database")),
            Succeeded: succeeded,
            Failed: failed,
            Duration: finishedAt - startedAt,
            ErrorSummary: errorSummary,
            Outcomes: outcomes);
    }
}
