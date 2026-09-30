using MediatR;
using Meezan.Application.Common;
using Meezan.Application.Common.Interfaces;

namespace Meezan.Application.Features.Indices.Queries.GetIndexConstituents;

public record GetIndexConstituentsQuery(
    string IndexCode,
    int? Page,
    int? PageSize,
    string? SortBy,
    string? SortDir,
    string? Search,
    string? ShariahStatus,
    string[]? ShariahStatuses = null,
    string? PriceComparison = null,
    int? MinCompliantSources = null,
    decimal? MinPeRatio = null,
    decimal? MaxPeRatio = null,
    decimal? MinPbRatio = null,
    decimal? MaxPbRatio = null
) : IRequest<IndexConstituentsPagedResultDto?>;

public record IndexConstituentsPagedResultDto(
    string IndexCode,
    string IndexNameAr,
    string IndexNameEn,
    DateTime? LastUpdated,
    List<ConstituentItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages
);

public record ConstituentItemDto(
    string Ticker,
    string? NameAr,
    string? NameEn,
    List<string> Indices,
    string? ShariahStatus,
    List<Meezan.Application.Features.Shariah.DTOs.ShariahSourceOpinionDto> ShariahOpinions,
    decimal? ClosingPrice,
    decimal? ChangePct,
    decimal? FairValue,
    string? PriceComparison,
    decimal? FairValueDiffPct,
    decimal? Weight,
    string? Currency,
    string? SectorNameAr = null,
    /// <summary>
    /// True when the stock is overseen by a Shariah board/committee — a plain
    /// "لجنة شرعية" and an accredited "هيئة رقابة شرعية داخلية معتمدة" are the same
    /// case. For these rows ShariahOpinions is empty and only ShariahBoardNote is surfaced.
    /// </summary>
    bool HasShariahBoard = false,
    string? ShariahBoardNote = null
);

public class GetIndexConstituentsQueryHandler : IRequestHandler<GetIndexConstituentsQuery, IndexConstituentsPagedResultDto?>
{
    private readonly IIndexRepository _indexRepo;

    public GetIndexConstituentsQueryHandler(IIndexRepository indexRepo)
    {
        _indexRepo = indexRepo;
    }

    public async Task<IndexConstituentsPagedResultDto?> Handle(
        GetIndexConstituentsQuery request, CancellationToken cancellationToken)
    {
        var index = await _indexRepo.GetByCodeAsync(request.IndexCode, cancellationToken);
        if (index == null) return null;

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
            // Board-governed stocks (plain "لجنة شرعية" or accredited
            // "هيئة رقابة شرعية داخلية معتمدة") are one and the same case: the external
            // opinion panel is dropped and only the unified board note is surfaced.
            bool hasBoard = Common.ShariahBoardDetector.IsBoardGoverned(
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
                    : p.ShariahOpinions,
                ClosingPrice: p.ClosingPrice,
                ChangePct: p.ChangePct,
                FairValue: p.FairValue,
                PriceComparison: p.PriceComparison,
                FairValueDiffPct: p.FairValueDiffPct,
                Weight: p.Weight,
                Currency: p.Currency,
                SectorNameAr: p.SectorNameAr,
                HasShariahBoard: hasBoard,
                ShariahBoardNote: Common.ShariahBoardDetector.NoteFor(hasBoard)
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
