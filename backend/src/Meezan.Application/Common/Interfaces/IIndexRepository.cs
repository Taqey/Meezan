using Meezan.Domain.Entities;
using Index = Meezan.Domain.Entities.Index;

namespace Meezan.Application.Common.Interfaces;

public interface IIndexRepository
{
    Task<Index?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string code, CancellationToken cancellationToken = default);
    Task UpdateAsync(Index index, CancellationToken cancellationToken = default);
    Task ReplaceConstituentsAsync(int indexId, IEnumerable<IndexConstituent> constituents, CancellationToken cancellationToken = default);

    Task<List<IndexSummaryProjection>> GetAllSummariesAsync(CancellationToken cancellationToken = default);
    Task<(List<ConstituentProjection> Items, int TotalCount)> QueryConstituentsPagedAsync(
        string indexCode, ConstituentListFilter filter, CancellationToken cancellationToken = default);
}

public record IndexSummaryProjection(
    string Code,
    string NameAr,
    string NameEn,
    string? Description,
    int ConstituentsCount,
    DateTime? LastUpdated
);

public record ConstituentProjection(
    string Ticker,
    string? NameAr,
    string? NameEn,
    string? IndexCodes,
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
    /// <summary>True when the stock is overseen by a Shariah board/committee.</summary>
    bool HasShariahBoard = false
);

public record ConstituentListFilter(
    int Page,
    int PageSize,
    string? SortBy,
    string? SortDir,
    string? Search,
    string? ShariahStatus,
    string? PriceComparison
);
