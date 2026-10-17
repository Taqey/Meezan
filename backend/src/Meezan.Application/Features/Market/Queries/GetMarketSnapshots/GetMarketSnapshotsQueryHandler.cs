using MediatR;
using Meezan.Application.Common.Interfaces;
using Meezan.Application.Features.Market.DTOs;

namespace Meezan.Application.Features.Market.Queries.GetMarketSnapshots;

public class GetMarketSnapshotsQueryHandler : IRequestHandler<GetMarketSnapshotsQuery, List<MarketSnapshotDto>>
{
    private readonly IMarketSnapshotRepository _snapshots;

    public GetMarketSnapshotsQueryHandler(IMarketSnapshotRepository snapshots)
    {
        _snapshots = snapshots;
    }

    public async Task<List<MarketSnapshotDto>> Handle(
        GetMarketSnapshotsQuery request, CancellationToken cancellationToken)
    {
        var rows = await _snapshots.GetLatestAsync(cancellationToken);
        return rows.Select(s => new MarketSnapshotDto(
            s.Kind, s.Code, s.MubasherCode, s.Value, s.Change, s.ChangePct,
            s.SourceDateText, s.Source, s.FetchedAt)).ToList();
    }
}
