using MediatR;
using Meezan.Application.Features.Market.Commands.RefreshMarketSnapshots;
using Meezan.Application.Features.Market.DTOs;
using Meezan.Application.Features.Market.Queries.GetMarketSnapshots;
using Microsoft.AspNetCore.Mvc;

namespace Meezan.WebApi.Controllers;

[ApiController]
[Route("api/market-snapshots")]
public class MarketSnapshotsController : ControllerBase
{
    private readonly ISender _sender;

    public MarketSnapshotsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Returns the latest-good published quotes (Mubasher) for indices and
    /// sectors. Empty when no successful fetch has run yet — callers fall back
    /// to computed values.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<MarketSnapshotDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetMarketSnapshotsQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Triggers a snapshots refresh on demand (same job the scheduler runs).
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshMarketSnapshotsResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _sender.Send(new RefreshMarketSnapshotsCommand(), cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
