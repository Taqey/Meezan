namespace Meezan.Application.Features.Stocks.DTOs;

public sealed record ConstituentManagementItemDto(
    int Id,
    int StockId,
    string Ticker,
    string? NameAr,
    int IndexId,
    string IndexCode,
    decimal? Weight,
    DateTime EffectiveDate);
