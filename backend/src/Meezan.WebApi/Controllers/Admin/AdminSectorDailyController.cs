using Meezan.Application.Common.Interfaces.Repositories.Sectors;
using Meezan.WebApi.Models.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Meezan.WebApi.Controllers.Admin;

[ApiController]
[Route("api/admin/sector-daily")]
[Authorize]
public class AdminSectorDailyController : ControllerBase
{
    private readonly ISectorDailyService _sectorDailyService;
    private readonly ILogger<AdminSectorDailyController> _logger;

    public AdminSectorDailyController(
        ISectorDailyService sectorDailyService,
        ILogger<AdminSectorDailyController> logger)
    {
        _sectorDailyService = sectorDailyService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the current status, settings, and latest calculated values for all sectors.
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var usdRate = await _sectorDailyService.GetUsdEgpRateAsync(cancellationToken);
        var weightingMethod = await _sectorDailyService.GetWeightingMethodAsync(cancellationToken);
        var externalPull = await _sectorDailyService.IsExternalSectorPullEnabledAsync(cancellationToken);
        var latestRows = await _sectorDailyService.GetLatestPerSectorAsync(cancellationToken);

        var latestTradingDate = latestRows.Select(r => (DateOnly?)r.TradingDate).Max();
        var lastComputedAt = latestRows.Select(r => (DateTime?)r.ComputedAt).Max();

        return Ok(new
        {
            UsdEgpRate = usdRate,
            WeightingMethod = weightingMethod,
            ExternalSectorPullEnabled = externalPull,
            ExternalSectorPullStatus = externalPull ? "يعمل" : "متوقف",
            LatestTradingDate = latestTradingDate,
            LastComputedAt = lastComputedAt,
            TotalSectors = latestRows.Count,
            Sectors = latestRows.Select(s => new
            {
                s.SectorId,
                s.TradingDate,
                s.ChangePct,
                MarketCapTotal = s.TotalMarketCap,
                Coverage = s.CoveragePct,
                s.Approximate,
                s.StocksUsed,
                s.StocksTotal,
                s.Advancers,
                s.Decliners,
                s.Unchanged,
                WeightShareRose = s.ShareOfWeightRose,
                Method = s.MethodUsed,
                s.TopContributorsJson,
                s.ComputedAt
            })
        });
    }

    /// <summary>
    /// Triggers immediate recalculation of all sectors for the latest trading date.
    /// </summary>
    [HttpPost("recalculate-today")]
    public async Task<IActionResult> RecalculateToday(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Admin requested immediate recalculation of daily sector changes for today/latest trading date.");
        var rows = await _sectorDailyService.RecalculateSectorsAsync(
            affectedSectorIds: null,
            reason: "AdminManualToday",
            ct: cancellationToken);

        return Ok(new
        {
            Success = true,
            Count = rows.Count,
            TradingDate = rows.FirstOrDefault()?.TradingDate,
            ComputedAt = rows.FirstOrDefault()?.ComputedAt
        });
    }

    /// <summary>
    /// Recomputes a date range (up to 30 days) using historical snapshots.
    /// </summary>
    [HttpPost("recompute-range")]
    public async Task<IActionResult> RecomputeRange(
        [FromBody] RecomputeRangeRequest request,
        CancellationToken cancellationToken)
    {
        if (request.FromDate > request.ToDate)
        {
            return BadRequest("fromDate cannot be greater than toDate.");
        }

        var days = request.ToDate.DayNumber - request.FromDate.DayNumber + 1;
        if (days > 31)
        {
            return BadRequest("Range cannot exceed 31 days.");
        }

        var recomputedCount = await _sectorDailyService.RecomputeDateRangeAsync(request.FromDate, request.ToDate, cancellationToken);

        return Ok(new
        {
            Success = true,
            DaysProcessed = days,
            RowsRecomputed = recomputedCount
        });
    }

    /// <summary>
    /// Updates USD_EGP_RATE, SECTOR_CHANGE_WEIGHTING, or EXTERNAL_SECTOR_PULL_ENABLED.
    /// Recalculates latest sectors if USD rate or weighting method changes.
    /// </summary>
    [HttpPut("config")]
    public async Task<IActionResult> UpdateConfig(
        [FromBody] UpdateSectorDailyConfigRequest request,
        CancellationToken cancellationToken)
    {
        bool needsRecalculate = false;

        if (request.UsdEgpRate.HasValue && request.UsdEgpRate.Value > 0)
        {
            await _sectorDailyService.SetUsdEgpRateAsync(request.UsdEgpRate.Value, cancellationToken);
            needsRecalculate = true;
        }

        if (!string.IsNullOrWhiteSpace(request.WeightingMethod))
        {
            await _sectorDailyService.SetWeightingMethodAsync(request.WeightingMethod, cancellationToken);
            needsRecalculate = true;
        }

        if (request.ExternalSectorPullEnabled.HasValue)
        {
            await _sectorDailyService.SetExternalSectorPullEnabledAsync(request.ExternalSectorPullEnabled.Value, cancellationToken);
        }

        if (needsRecalculate)
        {
            await _sectorDailyService.RecalculateSectorsAsync(
                affectedSectorIds: null,
                reason: "AdminConfigUpdate",
                ct: cancellationToken);
        }

        return Ok(new
        {
            Success = true,
            Recalculated = needsRecalculate,
            UsdEgpRate = await _sectorDailyService.GetUsdEgpRateAsync(cancellationToken),
            WeightingMethod = await _sectorDailyService.GetWeightingMethodAsync(cancellationToken),
            ExternalSectorPullEnabled = await _sectorDailyService.IsExternalSectorPullEnabledAsync(cancellationToken)
        });
    }
}
