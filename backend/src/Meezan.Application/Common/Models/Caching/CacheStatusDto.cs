namespace Meezan.Application.Common.Models.Caching;

public sealed record CacheStatusDto(
    bool RedisConnected,
    long CatalogVersion,
    long PricesVersion,
    int KeyCount,
    long ApproximateSizeBytes,
    long L1Hits,
    long L1Misses,
    long L2Hits,
    long L2Misses,
    DateTime? LastRebuildUtc,
    string KeyPrefix);
