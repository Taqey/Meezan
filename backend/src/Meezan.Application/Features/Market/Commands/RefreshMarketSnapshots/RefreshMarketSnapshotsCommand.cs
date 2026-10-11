using MediatR;

namespace Meezan.Application.Features.Market.Commands.RefreshMarketSnapshots;

/// <summary>
/// Refreshes the published market snapshots (one background run scrapes every
/// mapped index/sector page). Only successful parses overwrite the stored
/// rows, so readers always see the last good values.
/// </summary>
public record RefreshMarketSnapshotsCommand(string TriggeredBy = "Manual-MarketSnapshots") : IRequest<RefreshMarketSnapshotsResult>;

public record RefreshMarketSnapshotsResult(
    int Succeeded,
    int Failed,
    int TotalTargets,
    TimeSpan Duration);
