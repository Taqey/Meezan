using MediatR;
using Meezan.Application.Features.Funds.Commands.BackfillFundLogos;
using Meezan.Application.Features.Funds.Commands.CreateFundCategory;
using Meezan.Application.Features.Funds.Commands.DeleteFundCategory;
using Meezan.Application.Features.Funds.Commands.ExcludeFundFromCategory;
using Meezan.Application.Features.Funds.Commands.IncludeFundInCategory;
using Meezan.Application.Features.Funds.Commands.MergeFundCategories;
using Meezan.Application.Features.Funds.Commands.ResetCategoryOverrides;
using Meezan.Application.Features.Funds.Commands.ResetFundCategoryOverride;
using Meezan.Application.Features.Funds.Commands.ResetFundOverrides;
using Meezan.Application.Features.Funds.Commands.RestoreFundCategory;
using Meezan.Application.Features.Funds.Commands.RunFundSync;
using Meezan.Application.Features.Funds.Commands.UpdateFundCategory;
using Meezan.Application.Features.Funds.Commands.UpdateSyncSettings;
using Meezan.Application.Features.Funds.Queries.GetAdminFundById;
using Meezan.Application.Features.Funds.Queries.GetAdminFunds;
using Meezan.Application.Features.Funds.Queries.GetAllCategories;
using Meezan.Application.Features.Funds.Queries.GetCategoryById;
using Meezan.Application.Features.Funds.Queries.GetCategoryChanges;
using Meezan.Application.Features.Funds.Queries.GetCategoryFunds;
using Meezan.Application.Features.Funds.Queries.GetFundCategoryStatus;
using Meezan.Application.Features.Funds.Queries.GetFundCategoryOverrides;
using Meezan.Application.Features.Funds.Queries.GetLatestSyncRun;
using Meezan.Application.Features.Funds.Queries.GetSyncHistory;
using Meezan.Application.Features.Funds.Queries.GetSyncRunDetails;
using Meezan.Application.Features.Funds.Queries.GetSyncSettings;
using Meezan.Domain.Entities;
using Meezan.WebApi.Contracts.AdminFunds;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Meezan.WebApi.Controllers.Admin;

[ApiController]
[Authorize]
[Route("api/admin/funds")]
public class AdminFundsController : ControllerBase
{
    private readonly ISender _sender;

    public AdminFundsController(ISender sender)
    {
        _sender = sender;
    }

    // ========== FUND SYNC SETTINGS ==========

    [HttpGet("sync/settings")]
    [ProducesResponseType(typeof(FundSyncSettings), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSyncSettings(CancellationToken cancellationToken = default)
    {
        var settings = await _sender.Send(new GetSyncSettingsQuery(), cancellationToken);
        return Ok(settings);
    }

    [HttpPatch("sync/settings")]
    [ProducesResponseType(typeof(FundSyncSettings), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSyncSettings([FromBody] FundSyncSettingsUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var cmd = new UpdateSyncSettingsCommand
        {
            IsEnabled = request.IsEnabled,
            RunTimeCairo = request.RunTimeCairo,
            PreventOverlap = request.PreventOverlap,
            RequestDelaySeconds = request.RequestDelaySeconds,
            MaxRetries = request.MaxRetries,
            RetryBackoffSeconds = request.RetryBackoffSeconds,
            AutoIndexFundTypeEnabled = request.AutoIndexFundTypeEnabled,
            AutoClassificationEnabled = request.AutoClassificationEnabled
        };
        var settings = await _sender.Send(cmd, cancellationToken);
        return Ok(settings);
    }

    [HttpGet("sync/latest")]
    [ProducesResponseType(typeof(FundSyncLog), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLatestSyncRun(CancellationToken cancellationToken = default)
    {
        var run = await _sender.Send(new GetLatestSyncRunQuery(), cancellationToken);
        if (run == null) return NotFound();
        return Ok(run);
    }

    [HttpGet("sync/history")]
    [ProducesResponseType(typeof(SyncHistoryResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSyncHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetSyncHistoryQuery(page, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpGet("sync/{id:int}/details")]
    [ProducesResponseType(typeof(List<FundSyncLogDetail>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSyncRunDetails(int id, CancellationToken cancellationToken = default)
    {
        var details = await _sender.Send(new GetSyncRunDetailsQuery(id), cancellationToken);
        return Ok(details);
    }

    [HttpPost("sync/run")]
    [ProducesResponseType(typeof(FundSyncLog), StatusCodes.Status200OK)]
    public async Task<IActionResult> RunSyncNow(CancellationToken cancellationToken = default)
    {
        var log = await _sender.Send(new RunFundSyncCommand("Manual"), cancellationToken);
        return Ok(log);
    }

    // ========== FUND CATEGORIES ==========

    // POST /api/admin/funds/logos/backfill
    /// <summary>
    /// Downloads any fund logo that is not yet on disk and fills LocalLogoPath
    /// only for files that really landed. Safe to re-run.
    /// </summary>
    [HttpPost("logos/backfill")]
    [ProducesResponseType(typeof(BackfillFundLogosResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> BackfillLogos(CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new BackfillFundLogosCommand(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("categories")]
    [ProducesResponseType(typeof(List<FundCategory>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllCategories([FromQuery] bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var categories = await _sender.Send(new GetAllCategoriesQuery(includeDeleted), cancellationToken);
        return Ok(categories);
    }

    [HttpGet("categories/{id:int}")]
    [ProducesResponseType(typeof(FundCategory), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategory(int id, CancellationToken cancellationToken = default)
    {
        var category = await _sender.Send(new GetCategoryByIdQuery(id), cancellationToken);
        if (category == null) return NotFound();
        return Ok(category);
    }

    [HttpPost("categories")]
    [ProducesResponseType(typeof(FundCategory), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var cmd = new CreateFundCategoryCommand
        {
            NameAr = request.NameAr,
            NameEn = request.NameEn,
            Slug = request.Slug,
            Icon = request.Icon,
            Color = request.Color,
            IsAutoDerived = request.IsAutoDerived,
            IsIndexTracking = request.IsIndexTracking,
            AutoRuleJson = request.AutoRuleJson,
            IsVisible = request.IsVisible,
            DisplayOrder = request.DisplayOrder
        };
        var category = await _sender.Send(cmd, cancellationToken);
        return Ok(category);
    }

    [HttpPatch("categories/{id:int}")]
    [ProducesResponseType(typeof(FundCategory), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var cmd = new UpdateFundCategoryCommand
        {
            Id = id,
            NameAr = request.NameAr,
            NameEn = request.NameEn,
            Slug = request.Slug,
            Icon = request.Icon,
            Color = request.Color,
            IsIndexTracking = request.IsIndexTracking,
            IsVisible = request.IsVisible,
            DisplayOrder = request.DisplayOrder,
            AutoRuleJson = request.AutoRuleJson
        };
        var category = await _sender.Send(cmd, cancellationToken);
        return Ok(category);
    }

    [HttpDelete("categories/{id:int}")]
    [ProducesResponseType(typeof(DeleteFundCategoryResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCategory(int id, [FromQuery] bool confirm = false, CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new DeleteFundCategoryCommand(id, confirm), cancellationToken);
        return Ok(result);
    }

    [HttpPost("categories/{id:int}/restore")]
    [ProducesResponseType(typeof(FundCategory), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestoreCategory(int id, CancellationToken cancellationToken = default)
    {
        var category = await _sender.Send(new RestoreFundCategoryCommand(id), cancellationToken);
        return Ok(category);
    }

    [HttpPost("categories/merge")]
    [ProducesResponseType(typeof(MergeFundCategoriesResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> MergeCategories([FromBody] MergeCategoriesRequest request, CancellationToken cancellationToken = default)
    {
        var cmd = new MergeFundCategoriesCommand(request.SourceCategoryId, request.TargetCategoryId);
        var result = await _sender.Send(cmd, cancellationToken);
        return Ok(result);
    }

    // ========== FUND CATEGORY OVERRIDES ==========

    [HttpGet("funds/{fundId:int}/categories")]
    [ProducesResponseType(typeof(List<FundCategoryStatusDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFundCategories(int fundId, CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetFundCategoryStatusQuery(fundId), cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost("funds/{fundId:int}/categories/{categoryId:int}/include")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> IncludeFundInCategory(int fundId, int categoryId, CancellationToken cancellationToken = default)
    {
        await _sender.Send(new IncludeFundInCategoryCommand(fundId, categoryId), cancellationToken);
        return Ok();
    }

    [HttpPost("funds/{fundId:int}/categories/{categoryId:int}/exclude")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExcludeFundFromCategory(int fundId, int categoryId, CancellationToken cancellationToken = default)
    {
        await _sender.Send(new ExcludeFundFromCategoryCommand(fundId, categoryId), cancellationToken);
        return Ok();
    }

    [HttpDelete("funds/{fundId:int}/categories/{categoryId:int}/override")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetFundCategoryOverride(int fundId, int categoryId, CancellationToken cancellationToken = default)
    {
        await _sender.Send(new ResetFundCategoryOverrideCommand(fundId, categoryId), cancellationToken);
        return Ok();
    }

    [HttpGet("funds/{fundId:int}/category-overrides")]
    [ProducesResponseType(typeof(List<FundCategoryOverride>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFundCategoryOverrides(int fundId, CancellationToken cancellationToken = default)
    {
        var overrides = await _sender.Send(new GetFundCategoryOverridesQuery(fundId), cancellationToken);
        return Ok(overrides);
    }

    /// <summary>
    /// Funds currently in a category (final membership) with their source badge
    /// (Auto = derived by the sync, Manual = admin decision) and the
    /// auto-classification reason. Supports text search and an Auto-only filter.
    /// </summary>
    [HttpGet("categories/{id:int}/funds")]
    [ProducesResponseType(typeof(CategoryFundsResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategoryFunds(
        int id,
        [FromQuery] string? search,
        [FromQuery] string? source,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetCategoryFundsQuery(id, search, source, page, pageSize), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Clears a fund's MANUAL overrides (Reset to auto). Auto-derived rows are
    /// left for the sync to recalculate.
    /// </summary>
    [HttpDelete("funds/{fundId:int}/overrides")]
    [ProducesResponseType(typeof(ResetFundOverridesResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetFundOverrides(int fundId, CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new ResetFundOverridesCommand(fundId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Clears a category's MANUAL overrides (Reset to auto). Auto-derived rows
    /// are left for the sync to recalculate.
    /// </summary>
    [HttpDelete("categories/{id:int}/overrides")]
    [ProducesResponseType(typeof(ResetCategoryOverridesResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetCategoryOverrides(int id, CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new ResetCategoryOverridesCommand(id), cancellationToken);
        return Ok(result);
    }

    // ========== CATEGORY CHANGE LOG ==========

    [HttpGet("category-changes")]
    [ProducesResponseType(typeof(CategoryChangesResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategoryChanges(
        [FromQuery] int? fundId,
        [FromQuery] int? categoryId,
        [FromQuery] CategoryChangeAction? action,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = new GetCategoryChangesQuery(fundId, categoryId, action, fromDate, toDate, page, pageSize);
        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }

    // ========== FUND MANAGEMENT ==========

    [HttpGet("funds")]
    [ProducesResponseType(typeof(AdminFundsResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllFunds(
        [FromQuery] string? search,
        [FromQuery] string? type,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = new GetAdminFundsQuery(search, type, page, pageSize);
        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("funds/{id:int}")]
    [ProducesResponseType(typeof(Fund), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFund(int id, CancellationToken cancellationToken = default)
    {
        var fund = await _sender.Send(new GetAdminFundByIdQuery(id), cancellationToken);
        if (fund == null) return NotFound();
        return Ok(fund);
    }
}