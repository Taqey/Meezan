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
    private readonly Microsoft.Extensions.Options.IOptions<Meezan.Application.Common.Models.Caching.FundCacheOptions> _options;

    public CachedFundReadQueryDecorator(
        IFundReadQuery inner,
        IFundCacheService cache,
        Microsoft.Extensions.Options.IOptions<Meezan.Application.Common.Models.Caching.FundCacheOptions> options)
    {
        _inner = inner;
        _cache = cache;
        _options = options;
    }

    public async Task<FundStatsDto> GetStatsAsync(CancellationToken ct = default)
    {
        if (!_options.Value.Enabled)
        {
            return await _inner.GetStatsAsync(ct);
        }

        var dataset = await _cache.GetClassificationDatasetAsync(ct);
        var prices = await _cache.GetPricesSnapshotAsync(ct);

        // Best performer today: highest DailyChange among funds with non-null DailyChange
        var staticFunds = await _cache.GetStaticFundsAsync(ct);
        var best = prices.Values
            .Where(p => p.DailyChange != null && staticFunds.TryGetValue(p.FundId, out var sf) && sf.IsActive)
            .OrderByDescending(p => p.DailyChange)
            .Select(p => new
            {
                p.DailyChange,
                Code = staticFunds.TryGetValue(p.FundId, out var sf) ? sf.Code : null
            })
            .FirstOrDefault();

        if (best == null)
        {
            var innerStats = await _inner.GetStatsAsync(ct);
            return new FundStatsDto
            {
                TotalCount = dataset.Stats.TotalCount,
                IslamicCount = dataset.Stats.IslamicCount,
                TotalAum = dataset.Stats.TotalAum,
                BestDailyChange = innerStats.BestDailyChange,
                BestDailyChangeCode = innerStats.BestDailyChangeCode
            };
        }

        return new FundStatsDto
        {
            TotalCount = dataset.Stats.TotalCount,
            IslamicCount = dataset.Stats.IslamicCount,
            TotalAum = dataset.Stats.TotalAum,
            BestDailyChange = best != null ? (double?)best.DailyChange : null,
            BestDailyChangeCode = best?.Code
        };
    }

    public async Task<List<FundTypeOptionDto>> GetTypeOptionsAsync(CancellationToken ct = default)
    {
        if (!_options.Value.Enabled)
        {
            return await _inner.GetTypeOptionsAsync(ct);
        }

        var dataset = await _cache.GetClassificationDatasetAsync(ct);
        return dataset.TypeOptions;
    }

    public async Task<List<FundClassificationTreeDto>> GetClassificationTreeAsync(CancellationToken ct = default)
    {
        if (!_options.Value.Enabled)
        {
            return await _inner.GetClassificationTreeAsync(ct);
        }

        var dataset = await _cache.GetClassificationDatasetAsync(ct);
        return dataset.Tree;
    }

    public async Task<(Dictionary<int, (int TypeId, string Slug)> Overrides, Dictionary<string, Domain.Entities.FundTypeDefinition> BySlug)>
        LoadEffectiveTypesAsync(IReadOnlyCollection<int> fundIds, CancellationToken ct = default)
    {
        if (!_options.Value.Enabled)
        {
            return await _inner.LoadEffectiveTypesAsync(fundIds, ct);
        }

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

    public Task<Dictionary<int, Meezan.Application.Common.Models.Funds.FundPriceDto>> GetPricesForFundsAsync(
        IReadOnlyCollection<int> fundIds,
        CancellationToken ct = default)
    {
        // Prices are NEVER cached per fund and never part of the static entry. Always pass to inner SQL query.
        return _inner.GetPricesForFundsAsync(fundIds, ct);
    }

    public async Task<List<FundAssetManagerOptionDto>> GetAssetManagersAsync(CancellationToken ct = default)
    {
        if (!_options.Value.Enabled)
        {
            return await _inner.GetAssetManagersAsync(ct);
        }

        var dataset = await _cache.GetClassificationDatasetAsync(ct);
        return dataset.AssetManagers;
    }
}

