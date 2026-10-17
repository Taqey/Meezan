namespace Meezan.WebApi.Contracts.AdminStocksManagement;

public sealed record CreateStockRequest(
    string Ticker,
    string? NameAr,
    string? NameEn,
    int? SectorId);
