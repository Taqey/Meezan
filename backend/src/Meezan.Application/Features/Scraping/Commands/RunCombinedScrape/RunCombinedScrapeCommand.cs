using Meezan.Application.Common.Results;
using MediatR;

namespace Meezan.Application.Features.Scraping.Commands.RunCombinedScrape;

public record RunCombinedScrapeCommand(
    string TriggeredBy = "Manual",
    ScrapeDataBucket Bucket = ScrapeDataBucket.LiveDaily) : IRequest<Result<RunCombinedScrapeResult>>;
