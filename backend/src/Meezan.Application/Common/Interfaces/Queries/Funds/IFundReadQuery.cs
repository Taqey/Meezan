using Meezan.Application.Common.Models.Funds;
using Meezan.Application.Features.Funds.Classification.Dtos;
using Meezan.Application.Features.Funds.Listing.Dtos;
using Meezan.Application.Features.Funds.Types.Dtos;

namespace Meezan.Application.Common.Interfaces.Queries.Funds;

/// <summary>
/// Read-side queries for the public Funds endpoints.
/// Implementations live in Infrastructure and use AsNoTracking EF queries.
/// </summary>
public interface IFundReadQuery
{
    /// <summary>Returns KPI aggregates for the public fund stats strip.</summary>
    Task<FundStatsDto> GetStatsAsync(CancellationToken ct = default);

    /// <summary>Returns active, visible type definitions ordered by DisplayOrder.</summary>
    Task<List<FundTypeOptionDto>> GetTypeOptionsAsync(CancellationToken ct = default);

    /// <summary>Returns visible, non-deleted classification nodes with fund counts.</summary>
    Task<List<FundClassificationTreeDto>> GetClassificationTreeAsync(CancellationToken ct = default);

    /// <summary>Loads manual type overrides and active type definitions for the given funds.</summary>
    Task<(Dictionary<int, (int TypeId, string Slug)> Overrides, Dictionary<string, Domain.Entities.FundTypeDefinition> BySlug)>
        LoadEffectiveTypesAsync(IReadOnlyCollection<int> fundIds, CancellationToken ct = default);

    /// <summary>Loads price projections in a single SQL query for the requested fund IDs.</summary>
    Task<Dictionary<int, FundPriceDto>> GetPricesForFundsAsync(
        IReadOnlyCollection<int> fundIds,
        CancellationToken ct = default);

    /// <summary>Returns distinct asset managers with active fund counts.</summary>
    Task<List<FundAssetManagerOptionDto>> GetAssetManagersAsync(CancellationToken ct = default);
}
