using Meezan.Application.Common.Interfaces.Caching;
using Meezan.Application.Common.Interfaces.Queries.Funds;
using Meezan.Application.Features.Funds.Classification.Dtos;
using Meezan.Application.Features.Funds.Listing.Dtos;
using Meezan.Application.Features.Funds.Types.Dtos;

namespace Meezan.Infrastructure.Persistence.Queries.Funds;

public class CachedFundReadQueryDecorator : IFundReadQuery
{
    private readonly IFundReadQuery _inner;
    private readonly IFundCacheService _cache;

    public CachedFundReadQueryDecorator(IFundReadQuery inner, IFundCacheService cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public async Task<FundStatsDto> GetStatsAsync(CancellationToken ct = default)
    {
        var dataset = await _cache.GetClassificationDatasetAsync(ct);
        return dataset.Stats;
    }

    public async Task<List<FundTypeOptionDto>> GetTypeOptionsAsync(CancellationToken ct = default)
    {
        var dataset = await _cache.GetClassificationDatasetAsync(ct);
        return dataset.TypeOptions;
    }

    public async Task<List<FundClassificationTreeDto>> GetClassificationTreeAsync(CancellationToken ct = default)
    {
        var dataset = await _cache.GetClassificationDatasetAsync(ct);
        return dataset.Tree;
    }

    public async Task<(Dictionary<int, (int TypeId, string Slug)> Overrides, Dictionary<string, Domain.Entities.FundTypeDefinition> BySlug)>
        LoadEffectiveTypesAsync(IReadOnlyCollection<int> fundIds, CancellationToken ct = default)
    {
        var dataset = await _cache.GetClassificationDatasetAsync(ct);

        var overrides = dataset.TypeOverrides
            .Where(kv => fundIds.Contains(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        var bySlug = dataset.TypeDefsBySlug.ToDictionary(
            kv => kv.Key,
            kv => new Domain.Entities.FundTypeDefinition
            {
                Id = kv.Value.Id,
                Slug = kv.Value.Slug,
                NameAr = kv.Value.NameAr,
                NameEn = kv.Value.NameEn,
                DisplayOrder = kv.Value.DisplayOrder,
                IsVisible = kv.Value.IsVisible,
                IsDeleted = kv.Value.IsDeleted
            });

        return (overrides, bySlug);
    }
}
