using Meezan.Application.Common;
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

    // ── Shariah Manual Overrides ──────────────────────────────────────────────

    /// <summary>
    /// Gets the current Shariah metrics for a stock, including any manual overrides.
    /// </summary>
    [HttpGet("{ticker}/shariah-overrides")]
    [ProducesResponseType(typeof(ShariahOverridesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetShariahOverrides(
        [FromRoute] string ticker,
        CancellationToken cancellationToken)
    {
        var stock = await _context.Stocks
            .Include(s => s.ShariahMetrics)
            .FirstOrDefaultAsync(s => s.Ticker.ToUpper() == ticker.Trim().ToUpper(), cancellationToken);

        if (stock is null)
            return NotFound();

        var m = stock.ShariahMetrics;
        if (m is null)
            return NotFound("No Shariah metrics found for this stock.");

        return Ok(new ShariahOverridesDto(
            Ticker: stock.Ticker,
            // Feed values
            FeedCoreActivityCompliant: m.CoreActivityCompliant,
            FeedCategoryAr: m.CategoryAr,
            FeedCategoryEn: m.CategoryEn,
            FeedHaramRevenuePercentage: m.HaramEarningsPercentage,
            FeedLoansPercentage: m.LoansPercentage,
            // Override values
            OverrideCoreActivityCompliant: m.CoreActivityCompliantOverride,
            OverrideCategoryAr: m.CategoryArOverride,
            OverrideCategoryEn: m.CategoryEnOverride,
            OverrideHaramRevenuePercentage: m.HaramRevenuePercentageOverride,
            OverrideLoansPercentage: m.LoansPercentageOverride,
            // Effective values (override wins)
            EffectiveCoreActivityCompliant: m.EffectiveCoreActivityCompliant,
            EffectiveCategoryAr: m.EffectiveCategoryAr,
            EffectiveCategoryEn: m.EffectiveCategoryEn,
            EffectiveHaramRevenuePercentage: m.EffectiveHaramRevenuePercentage,
            EffectiveLoansPercentage: m.EffectiveLoansPercentage
        ));
    }

    /// <summary>
    /// Saves manual Shariah overrides for a stock. Null values clear the override (revert to feed).
    /// Validation: percentage overrides must be between 0 and 100.
    /// </summary>
    [HttpPatch("{ticker}/shariah-overrides")]
    [ProducesResponseType(typeof(ShariahOverridesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SaveShariahOverrides(
        [FromRoute] string ticker,
        [FromBody] SaveShariahOverridesRequest request,
        CancellationToken cancellationToken)
    {
        // Validate percentage ranges
        if (request.HaramRevenuePercentageOverride.HasValue &&
            (request.HaramRevenuePercentageOverride < 0 || request.HaramRevenuePercentageOverride > 100))
        {
            return BadRequest("Haram revenue percentage must be between 0 and 100.");
        }
        if (request.LoansPercentageOverride.HasValue &&
            (request.LoansPercentageOverride < 0 || request.LoansPercentageOverride > 100))
        {
            return BadRequest("Loans/interest percentage must be between 0 and 100.");
        }

        var stock = await _context.Stocks
            .Include(s => s.ShariahMetrics)
            .Include(s => s.ShariahCompliance)
            .FirstOrDefaultAsync(s => s.Ticker.ToUpper() == ticker.Trim().ToUpper(), cancellationToken);

        if (stock is null)
            return NotFound();

        var metrics = stock.ShariahMetrics;
        if (metrics is null)
            return NotFound("No Shariah metrics found for this stock.");

        // Apply overrides (null = clear override)
        metrics.CoreActivityCompliantOverride = request.CoreActivityCompliantOverride;
        metrics.CategoryArOverride = string.IsNullOrWhiteSpace(request.CategoryArOverride) ? null : request.CategoryArOverride.Trim();
        metrics.CategoryEnOverride = string.IsNullOrWhiteSpace(request.CategoryEnOverride) ? null : request.CategoryEnOverride.Trim();
        metrics.HaramRevenuePercentageOverride = request.HaramRevenuePercentageOverride;
        metrics.LoansPercentageOverride = request.LoansPercentageOverride;

        _context.StockShariahMetrics.Update(metrics);

        // Recompute compliance status immediately since effective values changed
        if (stock.ShariahCompliance != null)
        {
            var opinions = await _context.ShariahSourceOpinions
                .Where(o => o.StockId == stock.Id)
                .ToListAsync(cancellationToken);

            var isShariahIndexMember = stock.IndexConstituents.Any(ic =>
                ic.Index != null && ShariahStatusResolver.IsShariahIndexCode(ic.Index.Code));

            var newStatus = ShariahStatusResolver.EffectiveStatus(
                stock.ShariahCompliance, metrics, opinions, isShariahIndexMember);

            if (newStatus.HasValue && stock.ShariahCompliance.Status != newStatus.Value)
            {
                stock.ShariahCompliance.Status = newStatus.Value;
                stock.ShariahCompliance.UpdatedAt = DateTime.UtcNow;
                _context.ShariahCompliances.Update(stock.ShariahCompliance);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Return updated overrides
        return Ok(new ShariahOverridesDto(
            Ticker: stock.Ticker,
            FeedCoreActivityCompliant: metrics.CoreActivityCompliant,
            FeedCategoryAr: metrics.CategoryAr,
            FeedCategoryEn: metrics.CategoryEn,
            FeedHaramRevenuePercentage: metrics.HaramEarningsPercentage,
            FeedLoansPercentage: metrics.LoansPercentage,
            OverrideCoreActivityCompliant: metrics.CoreActivityCompliantOverride,
            OverrideCategoryAr: metrics.CategoryArOverride,
            OverrideCategoryEn: metrics.CategoryEnOverride,
            OverrideHaramRevenuePercentage: metrics.HaramRevenuePercentageOverride,
            OverrideLoansPercentage: metrics.LoansPercentageOverride,
            EffectiveCoreActivityCompliant: metrics.EffectiveCoreActivityCompliant,
            EffectiveCategoryAr: metrics.EffectiveCategoryAr,
            EffectiveCategoryEn: metrics.EffectiveCategoryEn,
            EffectiveHaramRevenuePercentage: metrics.EffectiveHaramRevenuePercentage,
            EffectiveLoansPercentage: metrics.EffectiveLoansPercentage
        ));
    }

    /// <summary>
    /// Resets a specific Shariah override field to feed value (clears the override).
    /// Field names: "CoreActivityCompliant", "CategoryAr", "CategoryEn", "HaramRevenuePercentage", "LoansPercentage"
    /// </summary>
    [HttpDelete("{ticker}/shariah-overrides/{fieldName}")]
    [ProducesResponseType(typeof(ShariahOverridesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetShariahOverride(
        [FromRoute] string ticker,
        [FromRoute] string fieldName,
        CancellationToken cancellationToken)
    {
        var stock = await _context.Stocks
            .Include(s => s.ShariahMetrics)
            .Include(s => s.ShariahCompliance)
            .FirstOrDefaultAsync(s => s.Ticker.ToUpper() == ticker.Trim().ToUpper(), cancellationToken);

        if (stock is null)
            return NotFound();

        var metrics = stock.ShariahMetrics;
        if (metrics is null)
            return NotFound("No Shariah metrics found for this stock.");

        // Clear the specific override field
        switch (fieldName.ToLowerInvariant())
        {
            case "coreactivitycompliant":
                metrics.CoreActivityCompliantOverride = null;
                break;
            case "categoryar":
                metrics.CategoryArOverride = null;
                break;
            case "categoryen":
                metrics.CategoryEnOverride = null;
                break;
            case "haramrevenuepercentage":
                metrics.HaramRevenuePercentageOverride = null;
                break;
            case "loanspercentage":
                metrics.LoansPercentageOverride = null;
                break;
            default:
                return BadRequest($"Unknown field '{fieldName}'. Valid fields: CoreActivityCompliant, CategoryAr, CategoryEn, HaramRevenuePercentage, LoansPercentage");
        }

        _context.StockShariahMetrics.Update(metrics);

        // Recompute compliance status
        if (stock.ShariahCompliance != null)
        {
            var opinions = await _context.ShariahSourceOpinions
                .Where(o => o.StockId == stock.Id)
                .ToListAsync(cancellationToken);

            var isShariahIndexMember = stock.IndexConstituents.Any(ic =>
                ic.Index != null && ShariahStatusResolver.IsShariahIndexCode(ic.Index.Code));

            var newStatus = ShariahStatusResolver.EffectiveStatus(
                stock.ShariahCompliance, metrics, opinions, isShariahIndexMember);

            if (newStatus.HasValue && stock.ShariahCompliance.Status != newStatus.Value)
            {
                stock.ShariahCompliance.Status = newStatus.Value;
                stock.ShariahCompliance.UpdatedAt = DateTime.UtcNow;
                _context.ShariahCompliances.Update(stock.ShariahCompliance);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new ShariahOverridesDto(
            Ticker: stock.Ticker,
            FeedCoreActivityCompliant: metrics.CoreActivityCompliant,
            FeedCategoryAr: metrics.CategoryAr,
            FeedCategoryEn: metrics.CategoryEn,
            FeedHaramRevenuePercentage: metrics.HaramEarningsPercentage,
            FeedLoansPercentage: metrics.LoansPercentage,
            OverrideCoreActivityCompliant: metrics.CoreActivityCompliantOverride,
            OverrideCategoryAr: metrics.CategoryArOverride,
            OverrideCategoryEn: metrics.CategoryEnOverride,
            OverrideHaramRevenuePercentage: metrics.HaramRevenuePercentageOverride,
            OverrideLoansPercentage: metrics.LoansPercentageOverride,
            EffectiveCoreActivityCompliant: metrics.EffectiveCoreActivityCompliant,
            EffectiveCategoryAr: metrics.EffectiveCategoryAr,
            EffectiveCategoryEn: metrics.EffectiveCategoryEn,
            EffectiveHaramRevenuePercentage: metrics.EffectiveHaramRevenuePercentage,
            EffectiveLoansPercentage: metrics.EffectiveLoansPercentage
        ));
    }

    // ── Existing endpoints ─────────────────────────────────────────────────────

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

        // Recalculate Fair Value if valuation inputs exist (Graham only for primary fields)
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

            // Graham only for primary fair value
            var grahamResult = FairValueCalculator.ComputeGrahamOnly(inputValues, marketData.ClosingPrice);
            // Individual methods for detail display
            var individualMethods = FairValueCalculator.ComputeIndividualMethods(
                inputValues, sectorMedians, stock.SectorId);

            calculatedFairValue = grahamResult.FairValue;

            var existingFv = await _context.StockFairValues
                .Include(fv => fv.Methods)
                .FirstOrDefaultAsync(fv => fv.StockId == stock.Id, cancellationToken);

            if (existingFv is null)
            {
                existingFv = new StockFairValue
                {
                    StockId = stock.Id,
                    FairValue = grahamResult.FairValue,
                    PriceComparison = grahamResult.Comparison,
                    FairValueDiff = grahamResult.DiffAbs,
                    FairValueDiffPct = grahamResult.DiffPct,
                    MethodsUsedCount = grahamResult.MethodsUsedCount,
                    MethodsExcludedCount = grahamResult.MethodsExcludedCount,
                    Confidence = grahamResult.Confidence,
                    ComputedAt = now
                };
                await _context.StockFairValues.AddAsync(existingFv, cancellationToken);
            }
            else
            {
                existingFv.FairValue = grahamResult.FairValue;
                existingFv.PriceComparison = grahamResult.Comparison;
                existingFv.FairValueDiff = grahamResult.DiffAbs;
                existingFv.FairValueDiffPct = grahamResult.DiffPct;
                existingFv.MethodsUsedCount = grahamResult.MethodsUsedCount;
                existingFv.MethodsExcludedCount = grahamResult.MethodsExcludedCount;
                existingFv.Confidence = grahamResult.Confidence;
                existingFv.ComputedAt = now;

                _context.StockFairValueMethods.RemoveRange(existingFv.Methods);
            }

            existingFv.Methods = individualMethods.Select(m => new StockFairValueMethod
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

// ── Shariah Override DTOs ────────────────────────────────────────────────────

/// <summary>Response DTO for Shariah manual overrides (feed, override, and effective values).</summary>
public sealed record ShariahOverridesDto(
    string Ticker,
    bool? FeedCoreActivityCompliant,
    string? FeedCategoryAr,
    string? FeedCategoryEn,
    decimal? FeedHaramRevenuePercentage,
    decimal? FeedLoansPercentage,
    bool? OverrideCoreActivityCompliant,
    string? OverrideCategoryAr,
    string? OverrideCategoryEn,
    decimal? OverrideHaramRevenuePercentage,
    decimal? OverrideLoansPercentage,
    bool? EffectiveCoreActivityCompliant,
    string? EffectiveCategoryAr,
    string? EffectiveCategoryEn,
    decimal? EffectiveHaramRevenuePercentage,
    decimal? EffectiveLoansPercentage);

/// <summary>Request body for saving Shariah manual overrides. Null values clear the override.</summary>
public sealed record SaveShariahOverridesRequest(
    bool? CoreActivityCompliantOverride,
    string? CategoryArOverride,
    string? CategoryEnOverride,
    decimal? HaramRevenuePercentageOverride,
    decimal? LoansPercentageOverride);
