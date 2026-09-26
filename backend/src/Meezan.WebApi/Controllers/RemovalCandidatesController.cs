using MediatR;
using Meezan.Application.Common;
using Meezan.Application.Common.Interfaces;
using Meezan.Application.Features.Scraping.Commands.RefreshSelectedStocks;
using Meezan.Domain.Enums;
using Meezan.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Meezan.WebApi.Controllers;

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
    private readonly ApplicationDbContext _context;
    private readonly IStockRepository _stockRepository;
    private readonly ISender _sender;

    public RemovalCandidatesController(
        ApplicationDbContext context,
        IStockRepository stockRepository,
        ISender sender)
    {
        _context = context;
        _stockRepository = stockRepository;
        _sender = sender;
    }

    /// <summary>
    /// Returns every stock that is stale, old, or flagged for any reason:
    /// soft-deactivated, non-Active DataStatus, missing market data, or a stale
    /// Mubasher last-update date. Read-only — listing a stock here changes nothing.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetRemovalCandidates(CancellationToken cancellationToken)
    {
        // 262-ish rows — a single lightweight query, filtered in memory because
        // staleness detection (Arabic date text) cannot be translated to SQL.
        var stocks = await _context.Stocks
            .AsNoTracking()
            .Include(s => s.Sector)
            .Include(s => s.MarketData)
            .OrderBy(s => s.Ticker)
            .ToListAsync(cancellationToken);

        var result = new List<RemovalCandidateDto>();
        foreach (var s in stocks)
        {
            string? reason = null;
            if (!s.IsActive)
                reason = "Deactivated";
            else if (s.DataStatus != StockDataStatus.Active)
                reason = "NotFoundOnSource";
            else if (s.MarketData is null)
                reason = "NotFoundOnSource";
            else if (MubasherStaleDetector.IsStale(s.MarketData.SourceLastUpdateText))
                reason = "StaleData";

            if (reason is null) continue;

            result.Add(new RemovalCandidateDto(
                StockId: s.Id,
                Ticker: s.Ticker,
                NameAr: s.NameAr,
                NameEn: s.NameEn,
                LastSuccessfulUpdate: s.MarketData?.FetchedAt,
                SourceLastUpdateText: s.MarketData?.SourceLastUpdateText,
                LastClosingPrice: s.MarketData?.ClosingPrice,
                Reason: reason,
                CurrentIsActive: s.IsActive,
                DataStatus: s.DataStatus.ToString(),
                DeactivatedAt: s.DeactivatedAt,
                DeactivationReason: s.DeactivationReason,
                SectorNameAr: s.Sector?.NameAr));
        }

        return Ok(result);
    }

    /// <summary>
    /// Deactivates ONLY the explicitly checked stocks (IsActive = false).
    /// Rows and all child data are retained and can be reactivated.
    /// </summary>
    [HttpPost("confirm")]
    public async Task<IActionResult> ConfirmRemovals(
        [FromBody] RemovalCandidatesSelection request,
        CancellationToken cancellationToken)
    {
        var tickers = Normalise(request?.Tickers);
        if (tickers.Count == 0)
            return BadRequest(new { message = "No tickers supplied." });

        var confirmed = new List<string>();
        var notFound = new List<string>();

        foreach (var ticker in tickers)
        {
            var stock = await _context.Stocks
                .FirstOrDefaultAsync(s => s.Ticker.ToUpper() == ticker, cancellationToken);
            if (stock is null)
            {
                notFound.Add(ticker);
                continue;
            }

            if (stock.IsActive)
                await _stockRepository.DeactivateAsync(
                    stock.Id, request?.Reason ?? "Confirmed by operator via removal-candidates checklist", cancellationToken);
            confirmed.Add(stock.Ticker);
        }

        return Ok(new
        {
            message = $"Deactivated {confirmed.Count} stock(s). Rows retained with IsActive = false.",
            confirmed,
            notFound
        });
    }

    /// <summary>
    /// Re-scrapes ONLY the explicitly checked stocks and commits their fresh data
    /// (scoped upsert — every other stock is untouched). On success the stock's
    /// DataStatus is restored to Active. This is the "update now" action for
    /// checklist stocks whose data is old but worth refreshing.
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshSelected(
        [FromBody] RemovalCandidatesSelection request,
        CancellationToken cancellationToken)
    {
        var tickers = Normalise(request?.Tickers);
        if (tickers.Count == 0)
            return BadRequest(new { message = "No tickers supplied." });

        var result = await _sender.Send(
            new RefreshSelectedStocksCommand(tickers, "Manual-ChecklistRefresh"),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Reactivates previously deactivated stocks (IsActive = true) — explicit undo.
    /// </summary>
    [HttpPost("reactivate")]
    public async Task<IActionResult> ReactivateSelected(
        [FromBody] RemovalCandidatesSelection request,
        CancellationToken cancellationToken)
    {
        var tickers = Normalise(request?.Tickers);
        if (tickers.Count == 0)
            return BadRequest(new { message = "No tickers supplied." });

        var reactivated = new List<string>();
        var notFound = new List<string>();

        foreach (var ticker in tickers)
        {
            var stock = await _context.Stocks
                .FirstOrDefaultAsync(s => s.Ticker.ToUpper() == ticker, cancellationToken);
            if (stock is null)
            {
                notFound.Add(ticker);
                continue;
            }

            if (!stock.IsActive)
                await _stockRepository.ReactivateAsync(stock.Id, cancellationToken);
            reactivated.Add(stock.Ticker);
        }

        return Ok(new
        {
            message = $"Reactivated {reactivated.Count} stock(s) (IsActive = true).",
            reactivated,
            notFound
        });
    }

    private static List<string> Normalise(List<string>? tickers) =>
        (tickers ?? new List<string>())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim().ToUpperInvariant())
            .Distinct()
            .ToList();
}

/// <summary>One reviewable row in the operator checklist.</summary>
public sealed record RemovalCandidateDto(
    int StockId,
    string Ticker,
    string? NameAr,
    string? NameEn,
    DateTime? LastSuccessfulUpdate,
    string? SourceLastUpdateText,
    decimal? LastClosingPrice,
    /// <summary>StaleData | NotFoundOnSource | Deactivated</summary>
    string Reason,
    bool CurrentIsActive,
    string DataStatus,
    DateTime? DeactivatedAt,
    string? DeactivationReason,
    string? SectorNameAr);

/// <summary>Checkbox selection posted by the frontend.</summary>
public sealed class RemovalCandidatesSelection
{
    public List<string> Tickers { get; set; } = new();
    public string? Reason { get; set; }
}
