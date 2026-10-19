using Meezan.Application.Common.Models.Caching;

namespace Meezan.Application.Common.Interfaces.Caching;

public interface IFundCacheService
{
    Task<CachedClassificationDataset> GetClassificationDatasetAsync(CancellationToken ct = default);
    Task<Dictionary<int, CachedFundStatic>> GetStaticFundsAsync(CancellationToken ct = default);
    Task<Dictionary<int, CachedFundPriceSnapshot>> GetPricesSnapshotAsync(CancellationToken ct = default);
    Task<List<CachedPricePoint>> GetPriceHistoryAsync(int fundId, CancellationToken ct = default);

    Task InvalidateCatalogAsync(CancellationToken ct = default);
    Task InvalidatePricesAsync(CancellationToken ct = default);
    Task ClearFundCacheAsync(CancellationToken ct = default);
    Task RebuildAllAsync(CancellationToken ct = default);

    Task<CacheStatusDto> GetStatusAsync(CancellationToken ct = default);
}
