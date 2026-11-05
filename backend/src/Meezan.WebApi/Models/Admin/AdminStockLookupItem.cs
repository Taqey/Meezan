namespace Meezan.WebApi.Models.Admin;

public sealed record AdminStockLookupItem(
    string Ticker,
    string? NameAr,
    string? NameEn);
