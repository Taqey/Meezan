using MediatR;

namespace Meezan.Application.Features.Scraping.Commands.RunCombinedScrape;

public enum ScrapeDataBucket
{
    LiveDaily,
    SlowQuarterly
}

public record RunCombinedScrapeCommand(
    string TriggeredBy = "Manual",
    ScrapeDataBucket Bucket = ScrapeDataBucket.LiveDaily) : IRequest<RunCombinedScrapeResult>;

public record RunCombinedScrapeResult(
    bool Success,
    int TotalStocks,
    int Succeeded,
    int Failed,
    TimeSpan Duration,
    string? ErrorSummary
);
