using MediatR;
using Meezan.Application.Common.Interfaces;

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
        if (md is null) return null;

        var fv = stock.FairValue ?? await _fvRepo.GetByStockIdAsync(stock.Id, cancellationToken);

        List<FairValueMethodDto>? methodDtos = null;
        if (fv?.Methods is { Count: > 0 })
        {
            methodDtos = fv.Methods
                .Select(m => new FairValueMethodDto(m.MethodName, m.EstimatedValue ?? 0, m.IsOutlier))
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

        var opinions = stock.ShariahSourceOpinions
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

        var metricsDto = stock.ShariahMetrics is { } sm
            ? new Meezan.Application.Features.Shariah.DTOs.StockShariahMetricsDto
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
            }
            : null;

        return new MarketDataDto(
            Ticker: stock.Ticker,
            NameAr: stock.NameAr,
            NameEn: stock.NameEn,
            SectorNameAr: stock.Sector?.NameAr,
            SectorNameEn: stock.Sector?.NameEn,
            Indices: indices,
            ShariahStatus: stock.ShariahCompliance?.Status.ToString(),
            ShariahPct: stock.ShariahCompliance?.Pct,
            ShariahOpinions: opinions,
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
            ShariahMetrics: metricsDto
        );
    }
}
