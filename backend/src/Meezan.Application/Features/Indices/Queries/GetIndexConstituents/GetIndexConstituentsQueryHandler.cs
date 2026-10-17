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
using Meezan.Application.Common.Results;
using Meezan.Application.Features.Indices.DTOs;

namespace Meezan.Application.Features.Indices.Queries.GetIndexConstituents;

public class GetIndexConstituentsQueryHandler : IRequestHandler<GetIndexConstituentsQuery, Result<IndexConstituentsPagedResultDto>>
{
    private readonly IIndexRepository _indexRepo;

    public GetIndexConstituentsQueryHandler(IIndexRepository indexRepo)
    {
        _indexRepo = indexRepo;
    }

    public async Task<Result<IndexConstituentsPagedResultDto>> Handle(
        GetIndexConstituentsQuery request, CancellationToken cancellationToken)
    {
        var index = await _indexRepo.GetByCodeAsync(request.IndexCode, cancellationToken);
        if (index == null)
            return Error.NotFound("IndexNotFound", "");

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

        var (projections, totalCount) = await _indexRepo.QueryConstituentsPagedAsync(
            request.IndexCode, filter, cancellationToken);

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
            IndexCode: index.Code,
            IndexNameAr: index.NameAr,
            IndexNameEn: index.NameEn,
            LastUpdated: index.LastUpdated,
            Items: items,
            Page: page,
            PageSize: pageSize,
            TotalCount: totalCount,
            TotalPages: totalPages
        );
    }
}
