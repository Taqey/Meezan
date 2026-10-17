using MediatR;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.Sectors.DTOs;

namespace Meezan.Application.Features.Sectors.Queries.GetSectorsList;

/// <summary>
/// Returns all sectors that have at least one stock assigned,
/// sorted by stock count descending.
/// </summary>
public record GetSectorsListQuery : IRequest<Result<List<SectorSummaryDto>>>;

