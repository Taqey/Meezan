namespace Meezan.Application.Features.Scraping.DTOs;

public record FairValueMethodDto(
    string Name,
    decimal? Value,
    bool IsOutlier
);
