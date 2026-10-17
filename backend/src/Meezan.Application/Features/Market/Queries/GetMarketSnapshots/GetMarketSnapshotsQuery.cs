using MediatR;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.Market.DTOs;

namespace Meezan.Application.Features.Market.Queries.GetMarketSnapshots;

/// <summary>Latest-good published quotes (Mubasher) for indices and sectors.</summary>
public record GetMarketSnapshotsQuery : IRequest<Result<List<MarketSnapshotDto>>>;
