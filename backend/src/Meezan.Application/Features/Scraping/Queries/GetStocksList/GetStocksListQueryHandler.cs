using Meezan.Application.Common.Results;
using MediatR;
using Meezan.Application.Common;
using Meezan.Application.Common.Services.Text;
using Meezan.Application.Common.Services.Market;
using Meezan.Application.Common.Services.Shariah;
using Meezan.Application.Common.Interfaces.Integrations;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Interfaces.Queries.Funds;
using Meezan.Application.Common.Interfaces.Queries.RemovalCandidates;
using Meezan.Application.Common.Interfaces.Queries.Stocks;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Interfaces.Repositories.Indices;
using Meezan.Application.Common.Interfaces.Repositories.Market;
using Meezan.Application.Common.Interfaces.Repositories.Scraping;
using Meezan.Application.Common.Interfaces.Repositories.Sectors;
using Meezan.Application.Common.Interfaces.Repositories.Shariah;
using Meezan.Application.Common.Interfaces.Repositories.Stocks;
using Meezan.Application.Common.Interfaces.Repositories.System;
using Meezan.Application.Common.Interfaces.Security;
using Meezan.Application.Common.Interfaces.Storage;
using Meezan.Application.Features.Scraping.DTOs;

namespace Meezan.Application.Features.Scraping.Queries.GetStocksList;

public class GetStocksListQueryHandler
    : IRequestHandler<GetStocksListQuery, Result<PagedResult<StockListItemDto>>>
{
    private readonly IStockRepository _stockRepo;

    public GetStocksListQueryHandler(IStockRepository stockRepo)
    {
        _stockRepo = stockRepo;
    }

    public async Task<Result<PagedResult<StockListItemDto>>> Handle(
        GetStocksListQuery request,
        CancellationToken cancellationToken)
    {
        var page     = PageSizeHelper.ClampPage(request.Page);
        var pageSize = PageSizeHelper.Clamp(request.PageSize);

        var filter = new StockListFilter(
            Page:            page,
            PageSize:        pageSize,
            SortBy:          request.SortBy,
            SortDir:         request.SortDir,
            Search:          request.Search,
            IndexCode:       request.IndexCode,
            IndexCodes:      request.IndexCodes,
            SectorId:        request.SectorId,
            ShariahStatus:   request.ShariahStatus,
            ShariahStatuses: request.ShariahStatuses,
            PriceComparison: request.PriceComparison,
            MinCompliantSources: request.MinCompliantSources,
            MinPeRatio:      request.MinPeRatio,
            MaxPeRatio:      request.MaxPeRatio,
            MinPbRatio:      request.MinPbRatio,
            MaxPbRatio:      request.MaxPbRatio
        );

        var (projections, totalCount) = await _stockRepo.QueryPagedAsync(filter, cancellationToken);

        var items = projections.Select(p => new StockListItemDto(
            Ticker:          p.Ticker,
            NameAr:          p.NameAr,
            NameEn:          p.NameEn,
            // Split the pipe-delimited index codes string back into a list
            Indices:         string.IsNullOrEmpty(p.IndexCodes)
                                 ? []
                                 : [.. p.IndexCodes.Split('|', StringSplitOptions.RemoveEmptyEntries)],
            ShariahStatus:   p.ShariahStatus,
            ClosingPrice:    p.ClosingPrice,
            ChangePct:       p.ChangePct,
            FairValue:       p.FairValue,
            PriceComparison: p.PriceComparison,
            FairValueDiffPct:p.FairValueDiffPct,
            Currency:        p.Currency,
            SectorNameAr:    p.SectorNameAr,
            PeRatio:         p.PeRatio,
            PbRatio:         p.PbRatio,
            Weight:          p.Weight,
            WeightIndexCode: p.WeightIndexCode,
            IsCapped:        p.IsCapped,
            CappingFactor:   p.CappingFactor,
            CoreActivityCompliant: p.CoreActivityCompliant,
            CategoryAr:      p.CategoryAr,
            CategoryEn:      p.CategoryEn,
            SpHaramEarningPercentage: p.SpHaramEarningPercentage,
            LoansPercentage: p.LoansPercentage,
            PurificationPct: p.PurificationPct,
            ViaScholarsOnly: p.ViaScholarsOnly,
            VerdictDetailsJson: p.VerdictDetailsJson
        )).ToList();

        return PagedResult<StockListItemDto>.Create(items, page, pageSize, totalCount);
    }
}
