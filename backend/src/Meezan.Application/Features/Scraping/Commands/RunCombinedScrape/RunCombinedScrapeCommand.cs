using MediatR;

namespace Meezan.Application.Features.Scraping.Commands.RunCombinedScrape;

public record RunCombinedScrapeCommand(
    string TriggeredBy = "Manual",
    ScrapeDataBucket Bucket = ScrapeDataBucket.LiveDaily) : IRequest<RunCombinedScrapeResult>;
