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
                // Fix 4: NonCompliant and board-governed stocks show no detailed metrics.
                // Activity-non-compliant stocks never do either (activity hard gate).
                ShariahMetrics: hasBoard1 || activityNonCompliant || effectiveStatus == ShariahStatus.NonCompliant
                    ? null
                    : BuildMetricsDto(stock.ShariahMetrics),
                HasShariahBoard: hasBoard1,
                ShariahBoardNote: boardNote1,
                ActivityCompliant: activityCompliant,
                ActivityClassification: activityClassification
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

        // ── Fix 4: Suppress ShariahMetrics for NonCompliant stocks ──────────────
        // For a stock already confirmed non-compliant, showing "0 جم/سهم" or "0%"
        // for every purification/haram-earnings metric is misleading noise.
        // Only include the full metrics panel for Compliant and Pending stocks being
        // actively evaluated against the standards. Board-governed stocks never show it:
        // purification is the board's own internal responsibility.
        var isNonCompliant = effectiveStatus == ShariahStatus.NonCompliant;
        var metricsDto = isNonCompliant || hasBoard || activityNonCompliant
            ? null
            : BuildMetricsDto(stock.ShariahMetrics);

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
            ActivityClassification: activityClassification
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

    private static Meezan.Application.Features.Shariah.DTOs.StockShariahMetricsDto? BuildMetricsDto(
        Meezan.Domain.Entities.StockShariahMetrics? sm)
    {
        if (sm is null) return null;
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
