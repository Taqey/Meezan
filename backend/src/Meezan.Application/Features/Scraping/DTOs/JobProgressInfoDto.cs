namespace Meezan.Application.Features.Scraping.DTOs;

public sealed record JobProgressInfoDto(
    string JobKey,
    string Status,
    LiveProgressInfoDto? Live,
    DateTime? FinishedAt = null,
    string? ErrorReason = null
);
