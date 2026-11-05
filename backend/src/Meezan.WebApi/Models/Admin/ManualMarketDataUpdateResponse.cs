namespace Meezan.WebApi.Models.Admin;

public sealed record ManualMarketDataUpdateResponse(
    string Ticker,
    bool LiveUpdated,
    bool SlowUpdated,
    DateTime FetchedAt,
    DateTime? SlowDataFetchedAt,
    decimal? FairValue = null);
