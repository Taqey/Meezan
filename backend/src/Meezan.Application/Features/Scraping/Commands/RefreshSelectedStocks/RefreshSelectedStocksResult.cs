namespace Meezan.Application.Features.Scraping.Commands.RefreshSelectedStocks;

public record RefreshSelectedStocksResult(
    bool Success,
    int TotalRequested,
    int TotalFound,
    int Succeeded,
    int Failed,
    TimeSpan Duration,
    string? ErrorSummary,
    List<SelectedStockRefreshOutcome> Outcomes);
