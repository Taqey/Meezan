namespace Meezan.WebApi.Models.Admin;

public sealed record ManualMarketDataUpdateRequest(
    decimal? NominalValue,
    decimal? MarketValue,
    decimal? BookValue,
    decimal? PbRatio,
    decimal? Eps,
    decimal? PeRatio,
    string? Currency,
    decimal? High,
    decimal? Low,
    decimal? Open,
    decimal? ClosingPrice,
    string? SourceLastUpdateText,
    decimal? SharesOutstanding = null,
    decimal? EarningsGrowthRate = null,
    decimal? EpsGrowthRate = null,
    decimal? FreeCashFlow = null,
    bool RecalculateRatios = true);
