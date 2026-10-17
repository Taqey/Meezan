using MediatR;
using Meezan.Application.Common.Interfaces.Integrations;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Interfaces.Queries.Funds;
using Meezan.Application.Common.Interfaces.Queries.RemovalCandidates;
using Meezan.Application.Common.Interfaces.Queries.Stocks;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Interfaces.Repositories.Indices;
using Meezan.Application.Common.Interfaces.Repositories.Market;
using Meezan.Application.Common.Interfaces.Repositories.Scraping;
using Meezan.Application.Common.Interfaces.Repositories.Sectors;
using Meezan.Application.Common.Interfaces.Repositories.Shariah;
using Meezan.Application.Common.Interfaces.Repositories.Stocks;
using Meezan.Application.Common.Interfaces.Repositories.System;
using Meezan.Application.Common.Interfaces.Security;
using Meezan.Application.Common.Interfaces.Storage;
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
