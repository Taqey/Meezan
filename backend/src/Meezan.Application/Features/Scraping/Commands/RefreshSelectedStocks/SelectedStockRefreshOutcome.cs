namespace Meezan.Application.Features.Scraping.Commands.RefreshSelectedStocks;

public record SelectedStockRefreshOutcome(
    string Ticker,
    bool Success,
    string? Message);
