namespace Meezan.Application.Features.Scraping.DTOs;

public sealed record LiveProgressInfoDto(
    string? TriggeredBy,
    int TotalStocks,
    int ProcessedCount,
    int SucceededCount,
    int FailedCount,
    double PercentComplete,
    string? CurrentStockTicker,
    DateTime? StartedAt,
    double ElapsedSeconds,
    double? EstimatedSecondsRemaining,
    string JobKey = "LiveDaily"
);
