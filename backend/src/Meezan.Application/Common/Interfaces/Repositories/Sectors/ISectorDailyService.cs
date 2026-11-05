using Meezan.Domain.Entities;

namespace Meezan.Application.Common.Interfaces.Repositories.Sectors;

public interface ISectorDailyService
{
    Task<decimal> GetUsdEgpRateAsync(CancellationToken ct = default);
    Task SetUsdEgpRateAsync(decimal rate, CancellationToken ct = default);
    Task<string> GetWeightingMethodAsync(CancellationToken ct = default);
    Task SetWeightingMethodAsync(string method, CancellationToken ct = default);
    Task<bool> IsExternalSectorPullEnabledAsync(CancellationToken ct = default);
    Task SetExternalSectorPullEnabledAsync(bool enabled, CancellationToken ct = default);

    /// <summary>
    /// Unified recalculation entry point called synchronously on any stock/market data changes.
    /// Recomputes stock market values, synchronizes daily snapshots, runs sector daily calculations,
    /// and invalidates static and sectoral caches.
    /// </summary>
    Task<List<SectorDaily>> RecalculateSectorsAsync(List<int>? affectedSectorIds, string reason, CancellationToken ct = default);

    Task<List<SectorDaily>> CalculateAsync(DateOnly? tradingDate = null, List<int>? specificSectorIds = null, string triggeredBy = "Manual", CancellationToken ct = default);

    /// <summary>
    /// Recomputes a date range using stored stock daily snapshots.
    /// </summary>
    Task<int> RecomputeDateRangeAsync(DateOnly fromDate, DateOnly toDate, CancellationToken ct = default);

    /// <summary>
    /// Returns the latest computed row for each sector.
    /// </summary>
    Task<List<SectorDaily>> GetLatestPerSectorAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns the latest computed row for a specific sector.
    /// </summary>
    Task<SectorDaily?> GetLatestForSectorAsync(int sectorId, CancellationToken ct = default);

    /// <summary>
    /// Gets history for a sector up to maxDays.
    /// </summary>
    Task<List<SectorDaily>> GetHistoryForSectorAsync(int sectorId, int maxDays = 30, CancellationToken ct = default);
}
