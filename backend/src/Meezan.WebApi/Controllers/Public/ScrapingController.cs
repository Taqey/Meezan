using MediatR;
using Meezan.Application.Features.Scraping.Commands.RunCombinedScrape;
using Meezan.Application.Features.Scraping.DTOs;
using Meezan.Application.Features.Scraping.Queries.GetScrapeStatus;
using Meezan.WebApi.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Meezan.WebApi.Controllers.Public;

[ApiController]
[Route("api/scraping")]
[Authorize]
public class ScrapingController : ControllerBase
{
    private readonly ISender _sender;

    public ScrapingController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Manually triggers a full market-data + support/resistance scrape for all stocks.
    /// This is a long-running operation — it runs synchronously and returns when complete.
    /// </summary>
    [HttpPost("run")]
    [ProducesResponseType(typeof(RunCombinedScrapeResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> RunScrape(
        [FromQuery] ScrapeDataBucket bucket = ScrapeDataBucket.LiveDaily,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new RunCombinedScrapeCommand(
                TriggeredBy: bucket == ScrapeDataBucket.LiveDaily ? "Manual-LiveDaily" : "Manual-SlowQuarterly",
                Bucket: bucket),
            cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>
    /// Returns the live progress of an active scrape run, or a summary of the
    /// last completed run when no run is in progress.
    ///
    /// Designed for polling every 2–3 seconds to drive a frontend progress bar.
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(ScrapeStatusResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatus(
        [FromQuery] string? jobKey,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetScrapeStatusQuery(jobKey), cancellationToken);
        return result.ToActionResult();
    }
}
