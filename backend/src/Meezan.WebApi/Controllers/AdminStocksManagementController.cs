using Meezan.Domain.Entities;
using Meezan.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Meezan.WebApi.Controllers;

/// <summary>
/// Admin endpoints for managing stocks, their sector assignments, and index memberships.
/// Changes here are reflected immediately on the public site.
/// Sector assignments made via this controller set IsManual = true,
/// which protects them from being overwritten by automated Excel uploads.
/// </summary>
[ApiController]
[Route("api/admin/stocks-management")]
public class AdminStocksManagementController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AdminStocksManagementController(ApplicationDbContext context)
    {
        _context = context;
    }

    // ── Stock search ──────────────────────────────────────────────────────────

    /// <summary>
    /// Search stocks by ticker or name (Arabic / English). Returns up to 50 results.
    /// Includes inactive stocks so deactivated tickers are still discoverable.
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(List<StockManagementItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] string? q, CancellationToken cancellationToken)
    {
        var query = _context.Stocks.AsNoTracking()
            .Include(s => s.Sector)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(s =>
                s.Ticker.Contains(term) ||
                (s.NameAr != null && s.NameAr.Contains(term)) ||
                (s.NameEn != null && s.NameEn.Contains(term)));
        }

        var list = await query
            .OrderBy(s => s.Ticker)
            .Take(50)
            .Select(s => new StockManagementItem(
                s.Id,
                s.Ticker,
                s.NameAr,
                s.NameEn,
                s.SectorId,
                s.Sector != null ? s.Sector.NameAr : null,
                s.Sector != null ? s.Sector.NameEn : null,
                s.IsManual,
                s.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(list);
    }

    // ── Create stock ──────────────────────────────────────────────────────────

    /// <summary>
    /// Create a new stock. If SectorId is provided the assignment is marked as manual.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(StockManagementItem), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateStock(
        [FromBody] CreateStockRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Ticker))
            return BadRequest("Ticker is required.");

        var ticker = request.Ticker.Trim().ToUpperInvariant();
        var existing = await _context.Stocks
            .FirstOrDefaultAsync(s => s.Ticker == ticker, cancellationToken);
        if (existing is not null)
            return Conflict($"A stock with ticker '{ticker}' already exists (Id={existing.Id}).");

        if (request.SectorId.HasValue)
        {
            var sectorExists = await _context.Sectors.AnyAsync(s => s.Id == request.SectorId.Value, cancellationToken);
            if (!sectorExists)
                return BadRequest($"Sector with Id {request.SectorId.Value} does not exist.");
        }

        var stock = new Stock
        {
            Ticker = ticker,
            NameAr = string.IsNullOrWhiteSpace(request.NameAr) ? null : request.NameAr.Trim(),
            NameEn = string.IsNullOrWhiteSpace(request.NameEn) ? null : request.NameEn.Trim(),
            SectorId = request.SectorId,
            IsManual = request.SectorId.HasValue,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Stocks.Add(stock);
        await _context.SaveChangesAsync(cancellationToken);

        var sector = request.SectorId.HasValue
            ? await _context.Sectors.FindAsync([request.SectorId.Value], cancellationToken)
            : null;

        var item = new StockManagementItem(
            stock.Id, stock.Ticker, stock.NameAr, stock.NameEn,
            stock.SectorId, sector?.NameAr, sector?.NameEn,
            stock.IsManual, stock.IsActive);

        return CreatedAtAction(nameof(Search), new { q = ticker }, item);
    }

    // ── Assign / move stock to sector ─────────────────────────────────────────

    /// <summary>
    /// Assign (or move) a stock to a sector. Sets IsManual = true to protect the
    /// assignment from being overwritten by future automated Excel uploads.
    /// </summary>
    [HttpPatch("{stockId:int}/sector")]
    [ProducesResponseType(typeof(StockManagementItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AssignSector(
        [FromRoute] int stockId,
        [FromBody] AssignSectorRequest request,
        CancellationToken cancellationToken)
    {
        var stock = await _context.Stocks
            .Include(s => s.Sector)
            .FirstOrDefaultAsync(s => s.Id == stockId, cancellationToken);
        if (stock is null) return NotFound();

        var sector = await _context.Sectors.FindAsync([request.SectorId], cancellationToken);
        if (sector is null)
            return BadRequest($"Sector with Id {request.SectorId} does not exist.");

        stock.SectorId = sector.Id;
        stock.Sector   = sector;
        stock.IsManual = true;
        stock.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new StockManagementItem(
            stock.Id, stock.Ticker, stock.NameAr, stock.NameEn,
            stock.SectorId, sector.NameAr, sector.NameEn,
            stock.IsManual, stock.IsActive));
    }

    // ── Remove stock from its sector ──────────────────────────────────────────

    /// <summary>
    /// Remove a stock from its current sector (sets SectorId = null).
    /// Also clears IsManual so the next upload can reassign it.
    /// </summary>
    [HttpDelete("{stockId:int}/sector")]
    [ProducesResponseType(typeof(StockManagementItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveFromSector(
        [FromRoute] int stockId,
        CancellationToken cancellationToken)
    {
        var stock = await _context.Stocks
            .FirstOrDefaultAsync(s => s.Id == stockId, cancellationToken);
        if (stock is null) return NotFound();

        stock.SectorId  = null;
        stock.Sector    = null;
        stock.IsManual  = false;
        stock.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new StockManagementItem(
            stock.Id, stock.Ticker, stock.NameAr, stock.NameEn,
            null, null, null, stock.IsManual, stock.IsActive));
    }

    // ── Sectors list ──────────────────────────────────────────────────────────

    /// <summary>Returns all sectors (including empty ones) for the admin picker.</summary>
    [HttpGet("sectors")]
    [ProducesResponseType(typeof(List<SectorPickerItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllSectors(CancellationToken cancellationToken)
    {
        var sectors = await _context.Sectors
            .AsNoTracking()
            .OrderBy(s => s.NameAr)
            .Select(s => new SectorPickerItem(s.Id, s.NameAr, s.NameEn))
            .ToListAsync(cancellationToken);
        return Ok(sectors);
    }

    // ── Indices list ──────────────────────────────────────────────────────────

    /// <summary>Returns all indices for the admin picker.</summary>
    [HttpGet("indices")]
    [ProducesResponseType(typeof(List<IndexPickerItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllIndices(CancellationToken cancellationToken)
    {
        var indices = await _context.Indices
            .AsNoTracking()
            .OrderBy(i => i.Code)
            .Select(i => new IndexPickerItem(i.Id, i.Code, i.NameAr, i.NameEn))
            .ToListAsync(cancellationToken);
        return Ok(indices);
    }

    // ── Index constituent management ──────────────────────────────────────────

    /// <summary>
    /// Add a stock to an index's constituents. If the stock is already a constituent
    /// the request is a no-op (returns 200 OK with the existing row).
    /// </summary>
    [HttpPost("{stockId:int}/indices/{indexId:int}")]
    [ProducesResponseType(typeof(ConstituentManagementItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddToIndex(
        [FromRoute] int stockId,
        [FromRoute] int indexId,
        [FromBody] AddToIndexRequest? request,
        CancellationToken cancellationToken)
    {
        var stock = await _context.Stocks.FindAsync([stockId], cancellationToken);
        if (stock is null) return NotFound("Stock not found.");

        var index = await _context.Indices.FindAsync([indexId], cancellationToken);
        if (index is null) return NotFound("Index not found.");

        var existing = await _context.IndexConstituents
            .FirstOrDefaultAsync(ic => ic.StockId == stockId && ic.IndexId == indexId, cancellationToken);

        if (existing is not null)
        {
            return Ok(new ConstituentManagementItem(existing.Id, stockId, stock.Ticker,
                stock.NameAr, indexId, index.Code, existing.Weight, existing.EffectiveDate));
        }

        // Sector weights are dynamic only — never store weight for sectoral indices.
        bool isSectoral = string.Equals(index.Code, "SECTORAL-INDICES", StringComparison.OrdinalIgnoreCase);

        var constituent = new IndexConstituent
        {
            StockId       = stockId,
            IndexId       = indexId,
            Weight        = isSectoral ? null : request?.Weight,
            EffectiveDate = request?.EffectiveDate ?? DateTime.UtcNow.Date
        };
        _context.IndexConstituents.Add(constituent);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new ConstituentManagementItem(constituent.Id, stockId, stock.Ticker,
            stock.NameAr, indexId, index.Code, constituent.Weight, constituent.EffectiveDate));
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
        var constituent = await _context.IndexConstituents
            .FirstOrDefaultAsync(ic => ic.StockId == stockId && ic.IndexId == indexId, cancellationToken);

        if (constituent is null) return NotFound();

        _context.IndexConstituents.Remove(constituent);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

// ── DTOs ──────────────────────────────────────────────────────────────────────

public sealed record StockManagementItem(
    int Id,
    string Ticker,
    string? NameAr,
    string? NameEn,
    int? SectorId,
    string? SectorNameAr,
    string? SectorNameEn,
    bool IsManual,
    bool IsActive);

public sealed record SectorPickerItem(int Id, string NameAr, string NameEn);

public sealed record IndexPickerItem(int Id, string Code, string NameAr, string NameEn);

public sealed record ConstituentManagementItem(
    int Id,
    int StockId,
    string Ticker,
    string? NameAr,
    int IndexId,
    string IndexCode,
    decimal? Weight,
    DateTime EffectiveDate);

public sealed record CreateStockRequest(
    string Ticker,
    string? NameAr,
    string? NameEn,
    int? SectorId);

public sealed record AssignSectorRequest(int SectorId);

public sealed record AddToIndexRequest(
    decimal? Weight,
    DateTime? EffectiveDate);
