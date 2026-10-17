using MediatR;
using Meezan.Application.Common.Results;

namespace Meezan.Application.Features.Scraping.Commands.RunCombinedScrape;

public record RunCombinedScrapeCommand(
    string TriggeredBy = "Manual",
    ScrapeDataBucket Bucket = ScrapeDataBucket.LiveDaily) : IRequest<Result<RunCombinedScrapeResult>>;
