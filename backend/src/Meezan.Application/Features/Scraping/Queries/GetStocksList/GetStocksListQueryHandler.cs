using MediatR;
using Meezan.Application.Common;
using Meezan.Application.Common.Interfaces;

namespace Meezan.Application.Features.Scraping.Queries.GetStocksList;

public class GetStocksListQueryHandler
    : IRequestHandler<GetStocksListQuery, PagedResult<StockListItemDto>>
{
    private readonly IStockRepository _stockRepo;

    public GetStocksListQueryHandler(IStockRepository stockRepo)
    {
        _stockRepo = stockRepo;
    }

    public async Task<PagedResult<StockListItemDto>> Handle(
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
            PbRatio:         p.PbRatio
        )).ToList();

        return PagedResult<StockListItemDto>.Create(items, page, pageSize, totalCount);
    }
}
