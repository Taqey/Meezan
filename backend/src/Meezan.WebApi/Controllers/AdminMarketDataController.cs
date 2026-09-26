using Meezan.Application.Features.Scraping.Services;
using Meezan.Domain.Entities;
using Meezan.Domain.Enums;
using Meezan.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Meezan.WebApi.Controllers;

[ApiController]
[Route("api/admin/market-data")]
public class AdminMarketDataController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AdminMarketDataController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("stocks")]
    [ProducesResponseType(typeof(List<AdminStockLookupItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllStocksForLookup(CancellationToken cancellationToken)
    {
        var list = await _context.Stocks
            .AsNoTracking()
            .OrderBy(s => s.Ticker)
            .Select(s => new AdminStockLookupItem(s.Ticker, s.NameAr, s.NameEn))
            .ToListAsync(cancellationToken);

        return Ok(list);
    }

    /// <summary>
    /// Lists all stocks with their current DataStatus.
    /// Useful for monitoring which stocks have been auto-flagged as IncompleteNoData.
    /// </summary>
    [HttpGet("data-status")]
    [ProducesResponseType(typeof(List<StockDataStatusItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllDataStatuses(CancellationToken cancellationToken)
    {
        var list = await _context.Stocks
            .AsNoTracking()
            .OrderBy(s => s.DataStatus)
            .ThenBy(s => s.Ticker)
            .Select(s => new StockDataStatusItem(s.Ticker, s.NameAr, s.NameEn, s.DataStatus.ToString(), s.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Ok(list);
    }

    /// <summary>
    /// Manually override a stock's DataStatus.
    /// Use this to re-activate a stock (Active), mark it as suspended (Suspended),
    /// or force-flag it as incomplete (IncompleteNoData).
    /// Valid values: Active, IncompleteNoData, Suspended.
    /// </summary>
    [HttpPatch("{ticker}/data-status")]
    [ProducesResponseType(typeof(StockDataStatusItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDataStatus(
        [FromRoute] string ticker,
        [FromBody] UpdateDataStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<StockDataStatus>(request.DataStatus, ignoreCase: true, out var newStatus))
        {
            return BadRequest($"Invalid DataStatus '{request.DataStatus}'. Valid values: Active, IncompleteNoData, Suspended.");
        }

        var stock = await _context.Stocks
            .FirstOrDefaultAsync(s => s.Ticker.ToUpper() == ticker.Trim().ToUpper(), cancellationToken);

        if (stock is null)
            return NotFound();

        stock.DataStatus = newStatus;
        stock.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new StockDataStatusItem(stock.Ticker, stock.NameAr, stock.NameEn, stock.DataStatus.ToString(), stock.UpdatedAt));
    }

    [HttpPatch("{ticker}")]
    [ProducesResponseType(typeof(ManualMarketDataUpdateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMarketData(
        [FromRoute] string ticker,
        [FromBody] ManualMarketDataUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var stock = await _context.Stocks
            .Include(s => s.MarketData)
            .FirstOrDefaultAsync(s => s.Ticker.ToUpper() == ticker.Trim().ToUpper(), cancellationToken);

        if (stock is null)
        {
            return NotFound();
        }

        var now = DateTime.UtcNow;
        var marketData = stock.MarketData;
        if (marketData is null)
        {
            marketData = new StockMarketData
            {
                StockId = stock.Id,
                FetchedAt = now
            };
            await _context.StockMarketData.AddAsync(marketData, cancellationToken);
        }

        marketData.NominalValue = request.NominalValue;
        marketData.MarketValue = request.MarketValue;
        marketData.BookValue = request.BookValue;
        marketData.Eps = request.Eps;
        marketData.PeRatio = request.PeRatio;
        marketData.PbRatio = request.PbRatio;
        marketData.Currency = request.Currency;

        marketData.High = request.High;
        marketData.Low = request.Low;
        marketData.Open = request.Open;
        marketData.ClosingPrice = request.ClosingPrice;
        marketData.SourceLastUpdateText = request.SourceLastUpdateText;

        if (request.RecalculateRatios)
        {
            marketData.PeRatio = CalculateRatio(marketData.ClosingPrice, marketData.Eps);
            marketData.PbRatio = CalculateRatio(marketData.ClosingPrice, marketData.BookValue);
        }

        marketData.FetchedAt = now;
        marketData.SlowDataFetchedAt = now;

        // Recalculate Fair Value if valuation inputs exist
        decimal? calculatedFairValue = null;
        if (marketData.ClosingPrice.HasValue || marketData.Eps.HasValue || marketData.BookValue.HasValue)
        {
            FairValueCalculator.SectorMedians? sectorMedians = null;
            if (stock.SectorId.HasValue)
            {
                var peers = await _context.Stocks
                    .Where(s => s.SectorId == stock.SectorId.Value && s.Id != stock.Id && s.MarketData != null)
                    .Select(s => new FairValueCalculator.SectorPeerData(
                        s.Id,
                        s.SectorId,
                        s.MarketData!.PeRatio,
                        s.MarketData!.PbRatio
                    ))
                    .ToListAsync(cancellationToken);

                var mediansDict = FairValueCalculator.ComputeSectorMedians(peers);
                mediansDict.TryGetValue(stock.SectorId.Value, out sectorMedians);
            }

            var inputValues = new FairValueCalculator.ScrapedMarketDataValues(
                Eps: marketData.Eps,
                PeRatio: marketData.PeRatio,
                BookValue: marketData.BookValue,
                PbRatio: marketData.PbRatio
            );

            var estimates = FairValueCalculator.BuildMethodEstimates(
                inputValues,
                sectorMedians,
                stock.SectorId
            );

            var fvResult = FairValueCalculator.Compute(estimates, marketData.ClosingPrice);

            // Persist Unavailable results too (FairValue null) — a skipped row used to read
            // back as a fabricated "قريبة من العادلة" verdict. calculatedFairValue stays null
            // so the response never reports a number that was not computed.
            calculatedFairValue = fvResult.FairValue;

            var existingFv = await _context.StockFairValues
                .Include(fv => fv.Methods)
                .FirstOrDefaultAsync(fv => fv.StockId == stock.Id, cancellationToken);

            if (existingFv is null)
            {
                existingFv = new StockFairValue
                {
                    StockId = stock.Id,
                    FairValue = fvResult.FairValue,
                    PriceComparison = fvResult.Comparison,
                    FairValueDiff = fvResult.DiffAbs,
                    FairValueDiffPct = fvResult.DiffPct,
                    MethodsUsedCount = fvResult.MethodsUsedCount,
                    MethodsExcludedCount = fvResult.MethodsExcludedCount,
                    Confidence = fvResult.Confidence,
                    ComputedAt = now
                };
                await _context.StockFairValues.AddAsync(existingFv, cancellationToken);
            }
            else
            {
                existingFv.FairValue = fvResult.FairValue;
                existingFv.PriceComparison = fvResult.Comparison;
                existingFv.FairValueDiff = fvResult.DiffAbs;
                existingFv.FairValueDiffPct = fvResult.DiffPct;
                existingFv.MethodsUsedCount = fvResult.MethodsUsedCount;
                existingFv.MethodsExcludedCount = fvResult.MethodsExcludedCount;
                existingFv.Confidence = fvResult.Confidence;
                existingFv.ComputedAt = now;

                _context.StockFairValueMethods.RemoveRange(existingFv.Methods);
            }

            existingFv.Methods = fvResult.Methods.Select(m => new StockFairValueMethod
            {
                StockFairValueId = existingFv.Id,
                MethodName = m.Name,
                EstimatedValue = m.Value,
                IsOutlier = m.IsOutlier
            }).ToList();
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new ManualMarketDataUpdateResponse(
            Ticker: stock.Ticker,
            LiveUpdated: true,
            SlowUpdated: true,
            FetchedAt: marketData.FetchedAt,
            SlowDataFetchedAt: marketData.SlowDataFetchedAt,
            FairValue: calculatedFairValue));
    }

    private static decimal? CalculateRatio(decimal? numerator, decimal? denominator)
    {
        if (!numerator.HasValue || !denominator.HasValue || denominator.Value <= 0)
        {
            return null;
        }

        return Math.Round(numerator.Value / denominator.Value, 4, MidpointRounding.AwayFromZero);
    }
}

public sealed record ManualMarketDataUpdateRequest(
    decimal? NominalValue,
    decimal? MarketValue,
    decimal? BookValue,
    decimal? PbRatio,
    decimal? Eps,
    decimal? PeRatio,
    string? Currency,
    decimal? High,
    decimal? Low,
    decimal? Open,
    decimal? ClosingPrice,
    string? SourceLastUpdateText,
    bool RecalculateRatios = true);

public sealed record ManualMarketDataUpdateResponse(
    string Ticker,
    bool LiveUpdated,
    bool SlowUpdated,
    DateTime FetchedAt,
    DateTime? SlowDataFetchedAt,
    decimal? FairValue = null);

public sealed record AdminStockLookupItem(
    string Ticker,
    string? NameAr,
    string? NameEn);

/// <summary>Snapshot of a stock's DataStatus for admin monitoring.</summary>
public sealed record StockDataStatusItem(
    string Ticker,
    string? NameAr,
    string? NameEn,
    string DataStatus,
    DateTime UpdatedAt);

/// <summary>Request body for the manual DataStatus override endpoint.</summary>
public sealed record UpdateDataStatusRequest(string DataStatus);
