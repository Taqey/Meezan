using MediatR;

namespace Meezan.Application.Features.Sectors.Queries.GetSectorsList;

/// <summary>
/// Returns all sectors that have at least one stock assigned,
/// sorted by stock count descending.
/// </summary>
public record GetSectorsListQuery : IRequest<List<SectorSummaryDto>>;

public record SectorSummaryDto(
    int Id,
    string NameAr,
    string NameEn,
    int StocksCount
);
