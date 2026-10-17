using MediatR;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.RemovalCandidates.Commands.ConfirmRemovals;
using Meezan.Application.Features.RemovalCandidates.Commands.ReactivateSelected;
using Meezan.Application.Features.RemovalCandidates.DTOs;
using Meezan.Application.Features.RemovalCandidates.Queries.GetRemovalCandidates;
using Meezan.Application.Features.Scraping.Commands.RefreshSelectedStocks;
using Meezan.WebApi.Common;
using Meezan.WebApi.Contracts.RemovalCandidates;
using Microsoft.AspNetCore.Mvc;

namespace Meezan.WebApi.Controllers.Public;

/// <summary>
/// Operator review checklist for stale / missing / deactivated stocks.
///
/// NOTHING in the scraping pipeline acts on these stocks automatically — a stock
/// appearing here is never updated, refreshed, or deactivated on its own. A human
/// operator explicitly chooses per stock (via frontend checkboxes):
///   - POST confirm  → soft-deactivate the checked stocks (IsActive = false).
///   - POST refresh  → re-scrape ONLY the checked stocks and commit their data.
///   - POST reactivate → restore previously deactivated stocks (IsActive = true).
/// </summary>
[ApiController]
[Route("api/scraping/removal-candidates")]
public class RemovalCandidatesController : ControllerBase
{
    private readonly ISender _sender;

    public RemovalCandidatesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Returns every stock that is stale, old, or flagged for any reason:
    /// soft-deactivated, non-Active DataStatus, missing market data, or a stale
    /// Mubasher last-update date. Read-only — listing a stock here changes nothing.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<RemovalCandidateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRemovalCandidates(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetRemovalCandidatesQuery(), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Deactivates ONLY the explicitly checked stocks (IsActive = false).
    /// Rows and all child data are retained and can be reactivated.
    /// </summary>
    [HttpPost("confirm")]
    [ProducesResponseType(typeof(ConfirmRemovalsResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmRemovals(
        [FromBody] RemovalCandidatesSelectionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new ConfirmRemovalsCommand(request?.Tickers, request?.Reason),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Re-scrapes ONLY the explicitly checked stocks and commits their fresh data
    /// (scoped upsert — every other stock is untouched). On success the stock's
    /// DataStatus is restored to Active. This is the "update now" action for
    /// checklist stocks whose data is old but worth refreshing.
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshSelectedStocksResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RefreshSelected(
        [FromBody] RemovalCandidatesSelectionRequest request,
        CancellationToken cancellationToken)
    {
        var tickers = (request?.Tickers ?? new List<string>())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim().ToUpperInvariant())
            .Distinct()
            .ToList();

        if (tickers.Count == 0)
        {
            return Result.Failure(Error.Validation("EmptyTickers", "No tickers supplied.", new { message = "No tickers supplied." })).ToActionResult();
        }

        var result = await _sender.Send(
            new RefreshSelectedStocksCommand(tickers, "Manual-ChecklistRefresh"),
            cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>
    /// Reactivates previously deactivated stocks (IsActive = true) — explicit undo.
    /// </summary>
    [HttpPost("reactivate")]
    [ProducesResponseType(typeof(ReactivateSelectedResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReactivateSelected(
        [FromBody] RemovalCandidatesSelectionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new ReactivateSelectedCommand(request?.Tickers),
            cancellationToken);
        return result.ToActionResult();
    }
}
