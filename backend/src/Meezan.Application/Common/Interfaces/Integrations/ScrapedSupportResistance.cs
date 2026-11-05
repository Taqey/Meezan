namespace Meezan.Application.Common.Interfaces.Integrations;

/// <summary>
/// Raw data returned by the Mubasher support/resistance page scraper for a single stock.
/// </summary>
public record ScrapedSupportResistance(
    string Ticker,
    decimal? LastPrice,
    decimal? ChangePct,
    decimal? Pivot,
    decimal? R1,
    decimal? R2,
    decimal? S1,
    decimal? S2
);
