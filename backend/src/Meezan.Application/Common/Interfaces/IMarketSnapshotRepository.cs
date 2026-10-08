using Meezan.Domain.Entities;

namespace Meezan.Application.Common.Interfaces;

/// <summary>Latest-good published market snapshots (Mubasher index/sector quotes).</summary>
public interface IMarketSnapshotRepository
{
    Task<List<MarketIndexSnapshot>> GetLatestAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts or replaces the snapshot for (Kind, Code). Persists immediately.
    /// </summary>
    Task UpsertAsync(MarketIndexSnapshot snapshot, CancellationToken cancellationToken = default);
}
