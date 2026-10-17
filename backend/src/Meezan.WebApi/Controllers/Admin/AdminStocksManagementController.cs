using MediatR;
using Meezan.Application.Features.Stocks.Commands.AddStockToIndex;
using Meezan.Application.Features.Stocks.Commands.AssignSector;
using Meezan.Application.Features.Stocks.Commands.CreateStock;
using Meezan.Application.Features.Stocks.Commands.RemoveFromSector;
using Meezan.Application.Features.Stocks.Commands.RemoveStockFromIndex;
using Meezan.Application.Features.Stocks.DTOs;
using Meezan.Application.Features.Stocks.Queries.GetIndexPickers;
using Meezan.Application.Features.Stocks.Queries.GetSectorPickers;
using Meezan.Application.Features.Stocks.Queries.SearchStocks;
using Meezan.WebApi.Common;
using Meezan.WebApi.Contracts.AdminStocksManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Meezan.WebApi.Controllers.Admin;

/// <summary>
/// Admin endpoints for managing stocks, their sector assignments, and index memberships.
/// Changes here are reflected immediately on the public site.
/// Sector assignments made via this controller set IsManual = true,
/// which protects them from being overwritten by automated Excel uploads.
/// </summary>
[ApiController]
[Route("api/admin/stocks-management")]
[Authorize]
public class AdminStocksManagementController : ControllerBase
{
    private readonly ISender _sender;

    public AdminStocksManagementController(ISender sender)
    {
        _sender = sender;
    }

    // ── Stock search ──────────────────────────────────────────────────────────

    /// <summary>
    /// Search stocks by ticker or name (Arabic / English). Returns up to 50 results.
    /// Includes inactive stocks so deactivated tickers are still discoverable.
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(List<StockManagementItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] string? q, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new SearchStocksQuery(q), cancellationToken);
        return result.ToActionResult();
    }

    // ── Create stock ──────────────────────────────────────────────────────────

    /// <summary>
    /// Create a new stock. If SectorId is provided the assignment is marked as manual.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(StockManagementItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateStock(
        [FromBody] CreateStockRequest request,
        CancellationToken cancellationToken)
    {
        var cmd = new CreateStockCommand(
            request.Ticker,
            request.NameAr,
            request.NameEn,
            request.SectorId);

        var result = await _sender.Send(cmd, cancellationToken);
        return result.ToCreatedResult(nameof(Search), result.IsSuccess ? new { q = result.Value.Ticker } : null);
    }

    // ── Assign / move stock to sector ─────────────────────────────────────────

    /// <summary>
    /// Assign (or move) a stock to a sector. Sets IsManual = true to protect the
    /// assignment from being overwritten by future automated Excel uploads.
    /// </summary>
    [HttpPatch("{stockId:int}/sector")]
    [ProducesResponseType(typeof(StockManagementItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AssignSector(
        [FromRoute] int stockId,
        [FromBody] AssignSectorRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new AssignSectorCommand(stockId, request.SectorId), cancellationToken);
        return result.ToActionResult();
    }

    // ── Remove stock from its sector ──────────────────────────────────────────

    /// <summary>
    /// Remove a stock from its current sector (sets SectorId = null).
    /// Also clears IsManual so the next upload can reassign it.
    /// </summary>
    [HttpDelete("{stockId:int}/sector")]
    [ProducesResponseType(typeof(StockManagementItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveFromSector(
        [FromRoute] int stockId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RemoveFromSectorCommand(stockId), cancellationToken);
        return result.ToActionResult();
    }

    // ── Sectors list ──────────────────────────────────────────────────────────

    /// <summary>Returns all sectors (including empty ones) for the admin picker.</summary>
    [HttpGet("sectors")]
    [ProducesResponseType(typeof(List<SectorPickerItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllSectors(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetSectorPickersQuery(), cancellationToken);
        return result.ToActionResult();
    }

    // ── Indices list ──────────────────────────────────────────────────────────

    /// <summary>Returns all indices for the admin picker.</summary>
    [HttpGet("indices")]
    [ProducesResponseType(typeof(List<IndexPickerItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllIndices(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetIndexPickersQuery(), cancellationToken);
        return result.ToActionResult();
    }

    // ── Index constituent management ──────────────────────────────────────────

    /// <summary>
    /// Add a stock to an index's constituents. If the stock is already a constituent
    /// the request is a no-op (returns 200 OK with the existing row).
    /// </summary>
    [HttpPost("{stockId:int}/indices/{indexId:int}")]
    [ProducesResponseType(typeof(ConstituentManagementItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddToIndex(
        [FromRoute] int stockId,
        [FromRoute] int indexId,
        [FromBody] AddToIndexRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new AddStockToIndexCommand(stockId, indexId, request?.Weight, request?.EffectiveDate),
            cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Remove a stock from an index's constituents.</summary>
    [HttpDelete("{stockId:int}/indices/{indexId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveFromIndex(
        [FromRoute] int stockId,
        [FromRoute] int indexId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RemoveStockFromIndexCommand(stockId, indexId), cancellationToken);
        return result.ToNoContentResult();
    }
}
