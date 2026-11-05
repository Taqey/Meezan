namespace Meezan.WebApi.Models.Admin;

/// <summary>Snapshot of a stock's DataStatus for admin monitoring.</summary>
public sealed record StockDataStatusItem(
    string Ticker,
    string? NameAr,
    string? NameEn,
    string DataStatus,
    DateTime UpdatedAt);
