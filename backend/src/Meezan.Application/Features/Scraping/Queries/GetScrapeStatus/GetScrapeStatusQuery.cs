using MediatR;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.Scraping.DTOs;

namespace Meezan.Application.Features.Scraping.Queries.GetScrapeStatus;

public record GetScrapeStatusQuery(string? JobKey = null) : IRequest<Result<ScrapeStatusResponseDto>>;
