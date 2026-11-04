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
    decimal? AveragePeRatio = null,
    string? TradingDate = null,
    DateTime? ComputedAt = null,
    bool Approximate = false,
    int StocksUsed = 0,
    int Advancers = 0,
    int Decliners = 0,
    int Unchanged = 0,
    string? TopContributorsJson = null
);
