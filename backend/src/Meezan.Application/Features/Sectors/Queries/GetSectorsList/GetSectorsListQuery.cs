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
    int StocksCount,
    /// <summary>Stocks whose effective verdict (activity → boards → standards upgrade → stored) is Compliant.</summary>
    int CompliantStocksCount = 0,
    /// <summary>100 × compliant / total, null when the sector has no listed stocks.</summary>
    decimal? ComplianceRatePct = null,
    /// <summary>Mean daily change % over stocks that have one; null when none do.</summary>
    decimal? AverageChangePct = null,
    /// <summary>Sum of market values (EGP) over stocks that have one; null when none do.</summary>
    decimal? MarketCapTotal = null,
    /// <summary>Sum of market values (EGP) over compliant stocks that have one; null when none do.</summary>
    decimal? CompliantMarketCapTotal = null,
    /// <summary>Mean P/E over stocks with a positive P/E; null when none do.</summary>
    decimal? AveragePeRatio = null
);
