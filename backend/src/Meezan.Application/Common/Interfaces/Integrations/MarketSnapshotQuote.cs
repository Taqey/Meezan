namespace Meezan.Application.Common.Interfaces.Integrations;

/// <summary>One parsed market-summary block from a Mubasher index/sector page.</summary>
public sealed record MarketSnapshotQuote(
    decimal? Value,
    decimal? Change,
    decimal? ChangePct,
    bool IsUp,
    string? SourceDateText);
