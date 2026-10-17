namespace Meezan.Application.Features.Scraping.DTOs;

public sealed record ScrapeStatusResponseDto(
    string Status,
    LiveProgressInfoDto? Live,
    LastRunSummaryDto? LastRun,
    Dictionary<string, JobProgressInfoDto>? Jobs = null
);
