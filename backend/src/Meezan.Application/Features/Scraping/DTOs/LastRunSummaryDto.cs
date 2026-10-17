namespace Meezan.Application.Features.Scraping.DTOs;

public sealed record LastRunSummaryDto(
    DateTime RunAt,
    DateTime? FinishedAt,
    int? DurationSeconds,
    int TotalStocks,
    int SucceededStocks,
    int FailedStocks,
    string TriggeredBy,
    bool HadErrors
);
