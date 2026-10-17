namespace Meezan.Application.Features.Scraping.DTOs;

public record RunCombinedScrapeResultDto(
    bool Success,
    int TotalStocks,
    int Succeeded,
    int Failed,
    TimeSpan Duration,
    string? ErrorSummary
);
