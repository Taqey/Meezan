using MediatR;
using Meezan.Application.Common;
using Meezan.Application.Common.Interfaces.Queries.Indices;
using Meezan.Application.Common.Interfaces.Repositories.Indices;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.Indices.DTOs;

namespace Meezan.Application.Features.Indices.Queries.GetIndexConstituents;

public class GetIndexConstituentsQueryHandler : IRequestHandler<GetIndexConstituentsQuery, Result<IndexConstituentsPagedResultDto>>
{
    private readonly IIndexConstituentReadQuery _query;

    public GetIndexConstituentsQueryHandler(IIndexConstituentReadQuery query)
    {
        _query = query;
    }

    public async Task<Result<IndexConstituentsPagedResultDto>> Handle(
        GetIndexConstituentsQuery request, CancellationToken cancellationToken)
    {

        int page = PageSizeHelper.ClampPage(request.Page);
        int pageSize = PageSizeHelper.Clamp(request.PageSize);

        var filter = new ConstituentListFilter(
            Page: page,
            PageSize: pageSize,
            SortBy: request.SortBy,
            SortDir: request.SortDir,
            Search: request.Search,
            ShariahStatus: request.ShariahStatus,
            ShariahStatuses: request.ShariahStatuses,
            PriceComparison: request.PriceComparison,
            MinCompliantSources: request.MinCompliantSources,
            MinPeRatio: request.MinPeRatio,
            MaxPeRatio: request.MaxPeRatio,
            MinPbRatio: request.MinPbRatio,
            MaxPbRatio: request.MaxPbRatio
        );

        var page_ = await _query.QueryPageAsync(request.IndexCode, filter, cancellationToken);
        if (page_ == null)
            return Error.NotFound("IndexNotFound", "");
        var projections = page_.Items;
        var totalCount = page_.TotalCount;

        var items = projections.Select(p =>
        {
            bool hasBoard = Common.Services.Shariah.ShariahBoardDetector.IsBoardGoverned(
                p.HasShariahBoard, p.ShariahOpinions.Select(o => o.Note));

            return new ConstituentItemDto(
                Ticker: p.Ticker,
                NameAr: p.NameAr,
                NameEn: p.NameEn,
                Indices: string.IsNullOrEmpty(p.IndexCodes)
                    ? new List<string>()
                    : p.IndexCodes.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList(),
                ShariahStatus: p.ShariahStatus,
                ShariahOpinions: hasBoard
                    ? new List<Meezan.Application.Features.Shariah.DTOs.ShariahSourceOpinionDto>()
                    : p.ShariahOpinions.Select(o => { o.Status = Common.Services.Shariah.ShariahVerdictService.SanitizeOpinionStatus(o.Status); return o; }).ToList(),
                ClosingPrice: p.ClosingPrice,
                ChangePct: p.ChangePct,
                FairValue: p.FairValue,
                PriceComparison: p.PriceComparison,
                FairValueDiffPct: p.FairValueDiffPct,
                Weight: p.Weight,
                Currency: p.Currency,
                SectorNameAr: p.SectorNameAr,
                HasShariahBoard: hasBoard,
                ShariahBoardNote: Common.Services.Shariah.ShariahBoardDetector.NoteFor(hasBoard)
            );
        }).ToList();

        int totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling((double)totalCount / pageSize);

        return new IndexConstituentsPagedResultDto(
            IndexCode: page_.IndexCode,
            IndexNameAr: page_.IndexNameAr,
            IndexNameEn: page_.IndexNameEn,
            LastUpdated: page_.LastUpdated,
            Items: items,
            Page: page,
            PageSize: pageSize,
            TotalCount: totalCount,
            TotalPages: totalPages
        );
    }
}
