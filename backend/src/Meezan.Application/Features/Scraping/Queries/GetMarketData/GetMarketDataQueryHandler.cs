using MediatR;
using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Enums;

namespace Meezan.Application.Features.Scraping.Queries.GetMarketData;

public class GetMarketDataQueryHandler : IRequestHandler<GetMarketDataQuery, MarketDataDto?>
{
    private readonly IStockRepository _stockRepo;
    private readonly IStockMarketDataRepository _mdRepo;
    private readonly IStockFairValueRepository _fvRepo;

    public GetMarketDataQueryHandler(
        IStockRepository stockRepo,
        IStockMarketDataRepository mdRepo,
        IStockFairValueRepository fvRepo)
    {
        _stockRepo = stockRepo;
        _mdRepo = mdRepo;
        _fvRepo = fvRepo;
    }

    public async Task<MarketDataDto?> Handle(GetMarketDataQuery request, CancellationToken cancellationToken)
    {
        var stock = await _stockRepo.GetByTickerWithFullDetailsAsync(request.Ticker.ToUpperInvariant(), cancellationToken);
        if (stock is null) return null;

        var md = stock.MarketData ?? await _mdRepo.GetByStockIdAsync(stock.Id, cancellationToken);

        // ── Activity hard gate (first screening) ─────────────────────────────────
        // "نشاط الشركة غير متوافق" disqualifies the stock on its own: it is not a
        // multi-factor judgement, so no board opinions, no purification % and no
        // AAOIFI/S&P ratios are assembled or returned for it — the UI shows a single
        // activity verdict instead. Nothing about how the flag itself is derived is
        // touched here.
        var activityCompliant = stock.ShariahMetrics?.CoreActivityCompliant;
        var activityNonCompliant = activityCompliant == false;
        var activityClassification = stock.ShariahMetrics?.CategoryAr ?? stock.ShariahMetrics?.CategoryEn;

        // ── Single source of truth for the displayed verdict ─────────────────────
        // This stock's own نشاط الشركة result wins over the stored ratio/board status,
        // so list, index-constituent and detail views always report the same value for
        // the same stock. The verdict is never derived from, or copied from, any other
        // stock — least of all from the sector/industry this stock belongs to.
        var effectiveStatus = Common.ShariahStatusResolver.EffectiveStatus(
            stock.ShariahCompliance, stock.ShariahMetrics, stock.ShariahSourceOpinions);

        // ── Fix 3: "Board exists only" stocks ────────────────────────────────────
        // Stocks imported via the Shariah source (stocks_merged.json) that are not
        // yet scrapable on Mubasher have no market data. Instead of returning null
        // (which maps to a 404 on the client), we return a minimal DTO carrying only
        // the stock's identity and Shariah data. The market-data fields are all null.
        // The frontend detects this via HasMarketData = false and renders a simplified
        // panel instead of the full trading detail page.
        if (md is null)
        {
            // Only worth returning a partial result if there is Shariah data to show.
            if (stock.ShariahCompliance is null && stock.ShariahSourceOpinions.Count == 0)
                return null;

            DetectShariahBoard(stock, out var hasBoard1, out var boardNote1);

            // Board-governed or activity-non-compliant: the 7-source opinion panel is
            // never assembled (activity gate wins over the board panel).
            var partialOpinions = hasBoard1 || activityNonCompliant
                ? new List<Meezan.Application.Features.Shariah.DTOs.ShariahSourceOpinionDto>()
                : stock.ShariahSourceOpinions
                .Select(o => new Meezan.Application.Features.Shariah.DTOs.ShariahSourceOpinionDto
                {
                    Id = o.Id,
                    StockId = o.StockId,
                    SourceKey = o.SourceKey,
                    Status = o.Status,
                    Percentage = o.Percentage,
                    Note = o.Note,
                    PdfUrl = o.PdfUrl,
                    SourceLastUpdated = o.SourceLastUpdated,
                    FetchedAt = o.FetchedAt,
                    ExtraData = o.ExtraData
                })
                .ToList();

            // Financial screens depend on WHY the stock is non-compliant, not on the
            // status alone: activity-non-compliant (impermissible core business) fails
            // the first screen so ratios are meaningless; permissible-activity stocks
            // (Compliant, Doubtful, or NonCompliant via financial ratios) must still
            // receive their metrics so the user sees which criterion failed.
            var partialMetrics = hasBoard1 || activityNonCompliant
                ? null
                : BuildMetricsDto(stock.ShariahMetrics);
            var partialReason = ResolveMetricsUnavailableReason(
                hasBoard1, activityNonCompliant, stock.ShariahMetrics, partialMetrics);

            return new MarketDataDto(
                Ticker: stock.Ticker,
                NameAr: stock.NameAr,
                NameEn: stock.NameEn,
                SectorNameAr: stock.Sector?.NameAr,
                SectorNameEn: stock.Sector?.NameEn,
                Indices: stock.IndexConstituents
                    .Where(ic => ic.Index != null)
                    .Select(ic => new IndexInStockDto(ic.Index!.Code, ic.Index.NameAr, ic.Index.NameEn, ic.Weight))
                    .ToList(),
                ShariahStatus: effectiveStatus?.ToString(),
                // Board-governed: purification is the board's own internal responsibility.
                // Activity-non-compliant: no ratio at all belongs on the page.
                ShariahPct: hasBoard1 || activityNonCompliant ? null : stock.ShariahCompliance?.Pct,
                ShariahOpinions: partialOpinions,
                HasMarketData: false,
                // All market-data fields are null
                NominalValue: null, MarketValue: null, BookValue: null, PbRatio: null,
                Eps: null, PeRatio: null, Currency: null, High: null, Low: null, Open: null,
                ClosingPrice: null, SourceLastUpdateText: null, FetchedAt: null,
                FairValue: null, PriceComparison: null, FairValueDiff: null,
                FairValueDiffPct: null, MethodsUsedCount: null, MethodsExcludedCount: null,
                ValuationConfidence: null, FairValueMethods: null,
                ShariahMetrics: partialMetrics,
                HasShariahBoard: hasBoard1,
                ShariahBoardNote: boardNote1,
                ActivityCompliant: activityCompliant,
                ActivityClassification: activityClassification,
                ShariahMetricsUnavailableReason: partialReason
            );
        }

        // ── Normal path: stock has market data ───────────────────────────────────
        var fv = stock.FairValue ?? await _fvRepo.GetByStockIdAsync(stock.Id, cancellationToken);

        List<FairValueMethodDto>? methodDtos = null;
        if (fv?.Methods is { Count: > 0 })
        {
            methodDtos = fv.Methods
                .Select(m => new FairValueMethodDto(m.MethodName, m.EstimatedValue, m.IsOutlier))
                .ToList();
        }

        var indices = stock.IndexConstituents
            .Where(ic => ic.Index != null)
            .Select(ic => new IndexInStockDto(
                ic.Index!.Code,
                ic.Index.NameAr,
                ic.Index.NameEn,
                ic.Weight
            ))
            .ToList();

        DetectShariahBoard(stock, out var hasBoard, out var boardNote);

        // Activity hard gate wins over the board panel too: a prohibited activity is a
        // standalone disqualification, so no external opinion is surfaced for the stock.
        var opinions = hasBoard || activityNonCompliant
            ? new List<Meezan.Application.Features.Shariah.DTOs.ShariahSourceOpinionDto>()
            : stock.ShariahSourceOpinions
            .Select(o => new Meezan.Application.Features.Shariah.DTOs.ShariahSourceOpinionDto
            {
                Id = o.Id,
                StockId = o.StockId,
                SourceKey = o.SourceKey,
                Status = o.Status,
                Percentage = o.Percentage,
                Note = o.Note,
                PdfUrl = o.PdfUrl,
                SourceLastUpdated = o.SourceLastUpdated,
                FetchedAt = o.FetchedAt,
                ExtraData = o.ExtraData
            })
            .ToList();

        // Financial screens depend on WHY the stock is non-compliant, not on the
        // status alone. A permissible-activity stock that is NonCompliant failed on
        // financial ratios, so its metrics MUST be returned (the user needs to see
        // which criterion failed). Only activity-non-compliant stocks (first screen
        // fails, ratios meaningless) and board-governed stocks (board's own
        // responsibility) suppress metrics. Never skip the calculation just because
        // the status is NonCompliant.
        var metricsDto = hasBoard || activityNonCompliant
            ? null
            : BuildMetricsDto(stock.ShariahMetrics);
        var metricsReason = ResolveMetricsUnavailableReason(
            hasBoard, activityNonCompliant, stock.ShariahMetrics, metricsDto);

        return new MarketDataDto(
            Ticker: stock.Ticker,
            NameAr: stock.NameAr,
            NameEn: stock.NameEn,
            SectorNameAr: stock.Sector?.NameAr,
            SectorNameEn: stock.Sector?.NameEn,
            Indices: indices,
            ShariahStatus: effectiveStatus?.ToString(),
            // Board-governed: purification is the board's own internal responsibility.
            // Activity-non-compliant: no ratio at all belongs on the page.
            ShariahPct: hasBoard || activityNonCompliant ? null : stock.ShariahCompliance?.Pct,
            ShariahOpinions: opinions,
            HasMarketData: true,
            NominalValue: md.NominalValue,
            MarketValue: md.MarketValue,
            BookValue: md.BookValue,
            PbRatio: md.PbRatio,
            Eps: md.Eps,
            PeRatio: md.PeRatio,
            Currency: md.Currency,
            High: md.High,
            Low: md.Low,
            Open: md.Open,
            ClosingPrice: md.ClosingPrice,
            SourceLastUpdateText: md.SourceLastUpdateText,
            FetchedAt: md.FetchedAt,
            FairValue: fv?.FairValue,
            PriceComparison: fv?.PriceComparison.ToString(),
            FairValueDiff: fv?.FairValueDiff,
            FairValueDiffPct: fv?.FairValueDiffPct,
            MethodsUsedCount: fv?.MethodsUsedCount,
            MethodsExcludedCount: fv?.MethodsExcludedCount,
            ValuationConfidence: fv?.Confidence.ToString(),
            FairValueMethods: methodDtos,
            ShariahMetrics: metricsDto,
            HasShariahBoard: hasBoard,
            ShariahBoardNote: boardNote,
            ActivityCompliant: activityCompliant,
            ActivityClassification: activityClassification,
            ShariahMetricsUnavailableReason: metricsReason
        );
    }

    /// <summary>
    /// Stocks supervised by their own Shariah board/committee are handled as a single case,
    /// whether the source calls it a plain "لجنة شرعية" or an accredited
    /// "هيئة رقابة شرعية داخلية معتمدة". Detection lives in
    /// <see cref="Common.ShariahBoardDetector"/>; the flag itself is persisted on
    /// ShariahCompliance.HasShariahBoard (populated from seed/source data).
    /// </summary>
    private static void DetectShariahBoard(
        Domain.Entities.Stock stock,
        out bool hasBoard,
        out string? boardNote)
    {
        hasBoard = Common.ShariahBoardDetector.IsBoardGoverned(
            stock.ShariahCompliance, stock.ShariahSourceOpinions);
        boardNote = Common.ShariahBoardDetector.NoteFor(hasBoard);
    }

    /// <summary>
    /// True when the metrics row carries no usable financial-screen inputs at all:
    /// no row, or every ratio field (debt, deposits, non-compliant income) is null.
    /// In that state we return null rather than inventing or estimating values.
    /// </summary>
    private static bool HasNoRatioData(Domain.Entities.StockShariahMetrics? sm)
    {
        if (sm is null) return true;
        return sm.LoansPercentage == null
            && sm.HaramEarningsPercentage == null
            && sm.SpHaramEarningPercentage == null
            && sm.AaoifiHaramEarningPerShare == null;
    }

    /// <summary>
    /// Distinguishes "not applicable" (screens intentionally not applied) from
    /// "not available" (inputs missing): board-governed or activity-non-compliant
    /// → NOT_CALCULATED; metrics null/empty → MISSING_FINANCIALS; otherwise null.
    /// </summary>
    private static string? ResolveMetricsUnavailableReason(
        bool hasBoard,
        bool activityNonCompliant,
        Domain.Entities.StockShariahMetrics? sm,
        Meezan.Application.Features.Shariah.DTOs.StockShariahMetricsDto? dto)
    {
        if (dto != null) return null;
        if (hasBoard || activityNonCompliant) return "NOT_CALCULATED";
        return "MISSING_FINANCIALS";
    }

    private static Meezan.Application.Features.Shariah.DTOs.StockShariahMetricsDto? BuildMetricsDto(
        Meezan.Domain.Entities.StockShariahMetrics? sm)
    {
        if (sm is null) return null;
        // Never invent or estimate: without any ratio inputs there is nothing to screen.
        if (HasNoRatioData(sm)) return null;
        return new Meezan.Application.Features.Shariah.DTOs.StockShariahMetricsDto
        {
            Id = sm.Id,
            StockId = sm.StockId,
            Zakat = sm.Zakat,
            SpHaramEarningPercentage = sm.SpHaramEarningPercentage,
            AaoifiHaramEarningPerShare = sm.AaoifiHaramEarningPerShare,
            HaramEarningsPercentage = sm.HaramEarningsPercentage,
            LoansPercentage = sm.LoansPercentage,
            FairValueValuation = sm.FairValueValuation,
            BookValue = sm.BookValue,
            Profit = sm.Profit,
            Dividend = sm.Dividend,
            DividendType = sm.DividendType,
            CoreActivityCompliant = sm.CoreActivityCompliant,
            CashLiquidityCompliant = sm.CashLiquidityCompliant,
            HaramInvestmentsCompliant = sm.HaramInvestmentsCompliant,
            CategoryEn = sm.CategoryEn,
            CategoryAr = sm.CategoryAr,
            SourceUpdatedAt = sm.SourceUpdatedAt,
            FetchedAt = sm.FetchedAt
        };
    }
}
