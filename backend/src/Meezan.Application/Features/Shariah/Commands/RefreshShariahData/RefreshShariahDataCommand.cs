using MediatR;
using Meezan.Application.Common.Interfaces;
using Meezan.Application.Common.Models;
using Meezan.Domain.Entities;
using Meezan.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Meezan.Application.Features.Shariah.Commands.RefreshShariahData;

public record RefreshShariahDataCommand(string TriggeredBy = "Manual") : IRequest<RefreshShariahDataResult>;

public record RefreshShariahDataResult(
    bool Success,
    string? Message,
    int PctUpdatedCount,
    int SkippedNoValueCount,
    int SkippedNotFoundCount,
    int StocksFullyRefreshedCount);

public class RefreshShariahDataCommandHandler : IRequestHandler<RefreshShariahDataCommand, RefreshShariahDataResult>
{
    private readonly IShariahSourceClient _sourceClient;
    private readonly IStockRepository _stockRepository;
    private readonly IShariahComplianceRepository _complianceRepository;
    private readonly IShariahSourceOpinionRepository _opinionRepository;
    private readonly IStockShariahMetricsRepository _metricsRepository;
    private readonly IShariahRefreshLogRepository _logRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RefreshShariahDataCommandHandler> _logger;

    public RefreshShariahDataCommandHandler(
        IShariahSourceClient sourceClient,
        IStockRepository stockRepository,
        IShariahComplianceRepository complianceRepository,
        IShariahSourceOpinionRepository opinionRepository,
        IStockShariahMetricsRepository metricsRepository,
        IShariahRefreshLogRepository logRepository,
        IUnitOfWork unitOfWork,
        ILogger<RefreshShariahDataCommandHandler> logger)
    {
        _sourceClient = sourceClient;
        _stockRepository = stockRepository;
        _complianceRepository = complianceRepository;
        _opinionRepository = opinionRepository;
        _metricsRepository = metricsRepository;
        _logRepository = logRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<RefreshShariahDataResult> Handle(RefreshShariahDataCommand request, CancellationToken cancellationToken)
    {
        List<ExternalStockMergedDto> externalData;
        try
        {
            externalData = await _sourceClient.FetchMergedStocksAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Log run failure gracefully
            var failLog = new ShariahRefreshLog
            {
                RunAt = DateTime.UtcNow,
                TriggeredBy = request.TriggeredBy,
                PctUpdatedCount = 0,
                SkippedNoValueCount = 0,
                SkippedNotFoundCount = 0,
                StocksFullyRefreshedCount = 0
            };
            await _logRepository.AddAsync(failLog, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new RefreshShariahDataResult(false, $"Failed to fetch external shariah data: {ex.Message}", 0, 0, 0, 0);
        }

        int pctUpdatedCount = 0;
        int skippedNoValueCount = 0;
        int skippedNotFoundCount = 0;
        int stocksFullyRefreshedCount = 0;
        var now = DateTime.UtcNow;

        // Deduplicate external data by normalized symbol so duplicate items in feed don't conflict
        var distinctItems = externalData
            .Where(item => !string.IsNullOrWhiteSpace(item.Symbol))
            .GroupBy(item => item.Symbol!.Trim().ToUpperInvariant())
            .Select(g => g.Last())
            .ToList();

        foreach (var item in distinctItems)
        {
            var symbol = item.Symbol!.Trim().ToUpperInvariant();

            try
            {
                // 1. Find or create Stock
                var stock = await _stockRepository.GetBySymbolCodeAsync(symbol, cancellationToken);
                if (stock == null)
                {
                    stock = new Stock
                    {
                        Ticker = symbol,
                        // Only set names when the external source has real data.
                        // Leave null if the external name is empty — real names come from index upload.
                        NameAr = !string.IsNullOrWhiteSpace(item.NameAr) ? item.NameAr : null,
                        NameEn = !string.IsNullOrWhiteSpace(item.NameEn) ? item.NameEn : null,
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    await _stockRepository.AddAsync(stock, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
                else
                {
                    // Backfill names when missing or still holding the old symbol-code placeholder.
                    bool modified = false;
                    bool nameArMissing = string.IsNullOrWhiteSpace(stock.NameAr) || stock.NameAr == stock.Ticker;
                    bool nameEnMissing = string.IsNullOrWhiteSpace(stock.NameEn) || stock.NameEn == stock.Ticker;

                    if (nameArMissing && !string.IsNullOrWhiteSpace(item.NameAr))
                    {
                        stock.NameAr = item.NameAr;
                        modified = true;
                    }
                    if (nameEnMissing && !string.IsNullOrWhiteSpace(item.NameEn))
                    {
                        stock.NameEn = item.NameEn;
                        modified = true;
                    }
                    if (modified)
                    {
                        await _stockRepository.UpdateAsync(stock, cancellationToken);
                    }
                }

                // ---------------------------------------------------------------
                // Part B — Skip rules (evaluated before any write)
                // ---------------------------------------------------------------
                // Rule 1: Stock has its own internal Sharia board / committee.
                //   The board's own ruling is the authoritative verdict.  We must
                //   not overwrite it with scraped external data.
                var existingCompliance = await _complianceRepository.GetByStockIdAsync(stock.Id, cancellationToken);
                if (existingCompliance?.HasShariahBoard == true)
                {
                    _logger.LogInformation(
                        "[ShariahRefresh] Skipping {Symbol}: skipped: internal sharia board",
                        symbol);
                    skippedNoValueCount++;
                    continue;
                }

                // Rule 2: Stock's core activity is prohibited.
                //   Prohibited-activity stocks never pass the activity gate, so
                //   fetching or overwriting their financial ratios is meaningless.
                //   We skip the rest of the loop but leave the stored Non-Compliant
                //   verdict intact.
                // NOTE: Use effective value (override wins over feed) for the activity gate check.
                var existingMetrics = await _metricsRepository.GetByStockIdAsync(stock.Id, cancellationToken);
                var effectiveActivityCompliantForSkip = existingMetrics?.EffectiveCoreActivityCompliant ?? true;
                if (effectiveActivityCompliantForSkip == false)
                {
                    _logger.LogInformation(
                        "[ShariahRefresh] Skipping {Symbol}: skipped: prohibited activity",
                        symbol);
                    skippedNoValueCount++;
                    continue;
                }

                // 2. Upsert StockShariahMetrics (overwrite feed values, but preserve manual overrides)
                // Re-use the row we may have already fetched above to avoid a
                // redundant DB round-trip.
                var metrics = existingMetrics
                    ?? await _metricsRepository.GetByStockIdAsync(stock.Id, cancellationToken);
                // Snapshot effective ratios BEFORE the feed update so the pull can
                // recompute the stored verdict ONLY for stocks whose ratios changed.
                decimal? oldEffRevenue = metrics?.EffectiveHaramRevenuePercentage ?? metrics?.SpHaramEarningPercentage;
                decimal? oldEffDebt = metrics?.EffectiveLoansPercentage;
                bool? oldEffActivity = metrics?.EffectiveCoreActivityCompliant;
                if (metrics == null)
                {
                    metrics = new StockShariahMetrics
                    {
                        StockId = stock.Id,
                        Zakat = item.Zakat,
                        SpHaramEarningPercentage = item.SpHaramEarningPercentage,
                        AaoifiHaramEarningPerShare = item.AaoifiHaramEarningPerShare,
                        HaramEarningsPercentage = item.HaramEarningsPercentage,
                        LoansPercentage = item.LoansPercentage,
                        FairValueValuation = item.FairValueValuation,
                        BookValue = item.BookValue,
                        Profit = item.Profit,
                        Dividend = item.Dividend,
                        DividendType = item.DividendType,
                        CoreActivityCompliant = item.CoreActivityCompliant,
                        CashLiquidityCompliant = item.CashLiquidityCompliant,
                        HaramInvestmentsCompliant = item.HaramInvestmentsCompliant,
                        CategoryEn = item.Category,
                        CategoryAr = item.CategoryAr,
                        SourceUpdatedAt = item.UpdatedAt ?? item.LastUpdated,
                        FetchedAt = now
                        // Override columns remain null (use feed)
                    };
                    await _metricsRepository.AddAsync(metrics, cancellationToken);
                }
                else
                {
                    // Update feed values, but DO NOT overwrite manual overrides
                    metrics.Zakat = item.Zakat;
                    metrics.SpHaramEarningPercentage = item.SpHaramEarningPercentage;
                    metrics.AaoifiHaramEarningPerShare = item.AaoifiHaramEarningPerShare;
                    // Only update feed values if no manual override is set
                    if (!metrics.HaramRevenuePercentageOverride.HasValue)
                        metrics.HaramEarningsPercentage = item.HaramEarningsPercentage;
                    if (!metrics.LoansPercentageOverride.HasValue)
                        metrics.LoansPercentage = item.LoansPercentage;
                    metrics.FairValueValuation = item.FairValueValuation;
                    metrics.BookValue = item.BookValue;
                    metrics.Profit = item.Profit;
                    metrics.Dividend = item.Dividend;
                    metrics.DividendType = item.DividendType;
                    // CoreActivityCompliant: only update feed value if no manual override
                    if (!metrics.CoreActivityCompliantOverride.HasValue)
                        metrics.CoreActivityCompliant = item.CoreActivityCompliant;
                    metrics.CashLiquidityCompliant = item.CashLiquidityCompliant;
                    metrics.HaramInvestmentsCompliant = item.HaramInvestmentsCompliant;
                    // Category: only update feed value if no manual override
                    if (string.IsNullOrWhiteSpace(metrics.CategoryEnOverride))
                        metrics.CategoryEn = item.Category;
                    if (string.IsNullOrWhiteSpace(metrics.CategoryArOverride))
                        metrics.CategoryAr = item.CategoryAr;
                    metrics.SourceUpdatedAt = item.UpdatedAt ?? item.LastUpdated;
                    metrics.FetchedAt = now;
                    _metricsRepository.Update(metrics);
                }

                // 3. Upsert ShariahSourceOpinions (overwrite/replace for each source key)
                // FaisalBank and Osoul are deliberately NOT written here: since the manual
                // import (ImportFaisalOsoulOpinionsCommand) their published PDF reports are the
                // only source of truth, so this scraper must never touch those two SourceKeys.
                // Their "no opinion" state comes from having no row, exactly like every board.
                var existingOpinions = await _opinionRepository.GetByStockIdAsync(stock.Id, cancellationToken);
                // Snapshot NON-Halal votes BEFORE the upserts so the pull recomputes the
                // overall verdict when opinions change even if ratios did not. Raw Halal
                // changes alone never trigger it (Halal follows the stored ratio verdict).
                var oldVotes = existingOpinions
                    .Where(o => o.SourceKey != ShariahSourceKey.HalalBourse)
                    .ToDictionary(o => o.SourceKey, o => (Common.ShariahVerdictService.SanitizeOpinionStatus(o.Status), o.Percentage));
                var opinionsDict = existingOpinions.ToDictionary(o => o.SourceKey);

                await UpdateOrAddOpinionAsync(opinionsDict, stock.Id, ShariahSourceKey.HalalBourse, item.HalalBourse, now, cancellationToken);
                await UpdateOrAddOpinionAsync(opinionsDict, stock.Id, ShariahSourceKey.Musaffa, item.Musaffa, now, cancellationToken);
                await UpdateOrAddOpinionAsync(opinionsDict, stock.Id, ShariahSourceKey.Kashif, item.Kashif, now, cancellationToken);
                await UpdateOrAddOpinionAsync(opinionsDict, stock.Id, ShariahSourceKey.HalalInvest, item.HalalInvest, now, cancellationToken);
                await UpdateOrAddOpinionAsync(opinionsDict, stock.Id, ShariahSourceKey.Thndr, item.Thndr, now, cancellationToken);

                // 4. Update stored verdicts via the ONE shared service.
                // Halal (ratios) recomputes ONLY when the effective ratios changed;
                // overall recomputes when ratios OR source opinions changed (membership
                // is untouched by this pull) — only for this affected stock, as part
                // of this same import. A stock can flip compliant<->non-compliant or
                // insufficient<->verdict here. Safety: a failed recompute keeps the
                // previous stored value and never breaks the pull.
                var compliance = existingCompliance
                    ?? await _complianceRepository.GetByStockIdAsync(stock.Id, cancellationToken);
                decimal? newEffRevenue = metrics.EffectiveHaramRevenuePercentage ?? metrics.SpHaramEarningPercentage;
                decimal? newEffDebt = metrics.EffectiveLoansPercentage;
                bool? newEffActivity = metrics.EffectiveCoreActivityCompliant;
                bool ratiosChanged = Common.ShariahVerdictService.HalalInputsChanged(
                    oldEffRevenue, oldEffDebt, oldEffActivity, newEffRevenue, newEffDebt, newEffActivity);
                var newVotes = opinionsDict.Values
                    .Where(o => o.SourceKey != ShariahSourceKey.HalalBourse)
                    .ToDictionary(o => o.SourceKey, o => (Common.ShariahVerdictService.SanitizeOpinionStatus(o.Status), o.Percentage));
                bool opinionsChanged = Common.ShariahVerdictService.OpinionInputsChanged(oldVotes, newVotes);
                if (compliance == null)
                {
                    // No stored row yet: create one with the computed Halal verdict
                    // and the aggregated overall verdict.
                    try
                        {
                            var v0 = Common.ShariahVerdictService.Compute(newEffRevenue, newEffDebt, null, null, newEffActivity);
                            compliance = new ShariahCompliance
                            {
                                StockId = stock.Id,
                                Status = ShariahStatus.Pending,
                                Pct = item.SpHaramEarningPercentage,
                                Note = null,
                                HasShariahBoard = false,
                                ViaScholarsOnly = false,
                                VerdictDetailsJson = null,
                                HalalStatus = null,
                                VerdictSource = null,
                                LastCheckedAt = now,
                                UpdatedAt = now
                            };
                            Common.ShariahVerdictService.TryApplyHalal(compliance, v0, _logger);
                            bool member0 = await _stockRepository.IsShariahIndexMemberAsync(stock.Id, cancellationToken);
                            Common.ShariahVerdictService.ApplyOverall(compliance, newEffActivity, opinionsDict.Values, member0, _logger);
                            await _complianceRepository.AddAsync(compliance, cancellationToken);
                        }
                    catch (Exception vex)
                    {
                        _logger.LogWarning(vex, "Verdict recompute failed for new stock {Symbol}; keeping no row.", symbol);
                        skippedNotFoundCount++;
                    }
                }
                else
                {
                    bool dirty = false;

                    bool hasBoardNow = Common.ShariahBoardDetector.IsBoardNote(compliance.Note)
                        || opinionsDict.Values.Any(o => Common.ShariahBoardDetector.IsBoardNote(o.Note));
                    if (compliance.HasShariahBoard != hasBoardNow)
                    {
                        compliance.HasShariahBoard = hasBoardNow;
                        dirty = true;
                    }

                    if (ratiosChanged)
                    {
                        try
                        {
                            var v = Common.ShariahVerdictService.Compute(newEffRevenue, newEffDebt, null, null, newEffActivity);
                            Common.ShariahVerdictService.TryApplyHalal(compliance, v, _logger);
                            dirty = true;
                        }
                        catch (Exception vex)
                        {
                            _logger.LogWarning(vex, "Halal verdict recompute failed for stock {Symbol}; keeping previous stored value.", symbol);
                        }
                    }

                    if (ratiosChanged || opinionsChanged)
                    {
                        try
                        {
                            bool member = await _stockRepository.IsShariahIndexMemberAsync(stock.Id, cancellationToken);
                            if (Common.ShariahVerdictService.ApplyOverall(compliance, newEffActivity, opinionsDict.Values, member, _logger))
                                dirty = true;
                        }
                        catch (Exception vex)
                        {
                            _logger.LogWarning(vex, "Overall verdict recompute failed for stock {Symbol}; keeping previous stored value.", symbol);
                        }
                    }

                    if (compliance.Status == ShariahStatus.Compliant && compliance.Pct == null)
                    {
                        if (item.SpHaramEarningPercentage.HasValue)
                        {
                            compliance.Pct = item.SpHaramEarningPercentage.Value;
                            dirty = true;
                            pctUpdatedCount++;
                        }
                        else
                        {
                            skippedNoValueCount++;
                        }
                    }

                    if (dirty)
                    {
                        compliance.UpdatedAt = now;
                        _complianceRepository.Update(compliance);
                    }
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                stocksFullyRefreshedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update shariah data for stock {Symbol}: {Message}", symbol, ex.Message);
            }
        }

        var log = new ShariahRefreshLog
        {
            RunAt = now,
            TriggeredBy = request.TriggeredBy,
            PctUpdatedCount = pctUpdatedCount,
            SkippedNoValueCount = skippedNoValueCount,
            SkippedNotFoundCount = skippedNotFoundCount,
            StocksFullyRefreshedCount = stocksFullyRefreshedCount
        };
        await _logRepository.AddAsync(log, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RefreshShariahDataResult(
            true,
            "Shariah data refreshed successfully",
            pctUpdatedCount,
            skippedNoValueCount,
            skippedNotFoundCount,
            stocksFullyRefreshedCount);
    }

    private async Task UpdateOrAddOpinionAsync(
        Dictionary<ShariahSourceKey, ShariahSourceOpinion> existingDict,
        int stockId,
        ShariahSourceKey key,
        ExternalSourceOpinionDto? dto,
        DateTime fetchedAt,
        CancellationToken cancellationToken)
    {
        if (dto == null) return;

        if (existingDict.TryGetValue(key, out var existing))
        {
            existing.Status = Common.ShariahVerdictService.SanitizeOpinionStatus(dto.Status);
            existing.Percentage = dto.Percentage;
            existing.Note = dto.Note;
            existing.PdfUrl = Common.ShariahPdfUrlNormalizer.Normalize(dto.PdfUrl);
            existing.SourceLastUpdated = dto.LastUpdated;
            existing.FetchedAt = fetchedAt;
            if (!string.IsNullOrEmpty(dto.ExtraDataJson))
            {
                existing.ExtraData = dto.ExtraDataJson;
            }
            _opinionRepository.Update(existing);
        }
        else
        {
            var opinion = new ShariahSourceOpinion
            {
                StockId = stockId,
                SourceKey = key,
                Status = Common.ShariahVerdictService.SanitizeOpinionStatus(dto.Status),
                Percentage = dto.Percentage,
                Note = dto.Note,
                PdfUrl = Common.ShariahPdfUrlNormalizer.Normalize(dto.PdfUrl),
                SourceLastUpdated = dto.LastUpdated,
                FetchedAt = fetchedAt,
                ExtraData = dto.ExtraDataJson
            };
            await _opinionRepository.AddAsync(opinion, cancellationToken);
            existingDict[key] = opinion;
        }
    }
}
