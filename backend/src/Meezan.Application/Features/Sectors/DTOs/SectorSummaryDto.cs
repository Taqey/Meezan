namespace Meezan.Application.Features.Sectors.DTOs;

public record SectorSummaryDto(
    int Id,
    string NameAr,
    string NameEn,
    int StocksCount,
    int CompliantStocksCount = 0,
    decimal? ComplianceRatePct = null,
    decimal? AverageChangePct = null,
    decimal? MarketCapTotal = null,
    decimal? CompliantMarketCapTotal = null,
    decimal? AveragePeRatio = null
);
