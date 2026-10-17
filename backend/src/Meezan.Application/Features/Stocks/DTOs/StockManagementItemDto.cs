namespace Meezan.Application.Features.Stocks.DTOs;

public sealed record StockManagementItemDto(
    int Id,
    string Ticker,
    string? NameAr,
    string? NameEn,
    int? SectorId,
    string? SectorNameAr,
    string? SectorNameEn,
    bool IsManual,
    bool IsActive);
