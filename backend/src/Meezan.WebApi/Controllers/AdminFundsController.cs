using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Meezan.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/admin/funds")]
public class AdminFundsController : ControllerBase
{
    private readonly IFundRepository _fundRepo;
    private readonly IFundPriceHistoryRepository _priceRepo;
    private readonly IFundAnalysisRepository _analysisRepo;
    private readonly IFundSimilarRepository _similarRepo;
    private readonly IFundCategoryRepository _categoryRepo;
    private readonly IFundCategoryOverrideRepository _overrideRepo;
    private readonly IFundSyncLogRepository _syncLogRepo;
    private readonly IFundSyncLogDetailRepository _syncDetailRepo;
    private readonly IFundSyncSettingsRepository _syncSettingsRepo;
    private readonly IFundCategoryChangeLogRepository _changeLogRepo;
    private readonly IFundSyncService _syncService;

    public AdminFundsController(
        IFundRepository fundRepo,
        IFundPriceHistoryRepository priceRepo,
        IFundAnalysisRepository analysisRepo,
        IFundSimilarRepository similarRepo,
        IFundCategoryRepository categoryRepo,
        IFundCategoryOverrideRepository overrideRepo,
        IFundSyncLogRepository syncLogRepo,
        IFundSyncLogDetailRepository syncDetailRepo,
        IFundSyncSettingsRepository syncSettingsRepo,
        IFundCategoryChangeLogRepository changeLogRepo,
        IFundSyncService syncService)
    {
        _fundRepo = fundRepo;
        _priceRepo = priceRepo;
        _analysisRepo = analysisRepo;
        _similarRepo = similarRepo;
        _categoryRepo = categoryRepo;
        _overrideRepo = overrideRepo;
        _syncLogRepo = syncLogRepo;
        _syncDetailRepo = syncDetailRepo;
        _syncSettingsRepo = syncSettingsRepo;
        _changeLogRepo = changeLogRepo;
        _syncService = syncService;
    }

    // ========== FUND SYNC SETTINGS ==========

    [HttpGet("sync/settings")]
    [ProducesResponseType(typeof(FundSyncSettings), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSyncSettings(CancellationToken cancellationToken = default)
    {
        var settings = await _syncSettingsRepo.GetAsync(cancellationToken);
        if (settings == null)
        {
            // Return defaults
            settings = new FundSyncSettings { Id = 1 };
        }
        return Ok(settings);
    }

    [HttpPatch("sync/settings")]
    [ProducesResponseType(typeof(FundSyncSettings), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSyncSettings([FromBody] FundSyncSettingsUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var settings = await _syncSettingsRepo.GetAsync(cancellationToken);
        if (settings == null)
        {
            settings = new FundSyncSettings { Id = 1 };
        }

        settings.IsEnabled = request.IsEnabled ?? settings.IsEnabled;
        if (request.RunTimeCairo.HasValue) settings.RunTimeCairo = request.RunTimeCairo.Value;
        settings.PreventOverlap = request.PreventOverlap ?? settings.PreventOverlap;
        settings.RequestDelaySeconds = request.RequestDelaySeconds ?? settings.RequestDelaySeconds;
        settings.MaxRetries = request.MaxRetries ?? settings.MaxRetries;
        settings.RetryBackoffSeconds = request.RetryBackoffSeconds ?? settings.RetryBackoffSeconds;
        if (request.AutoIndexFundTypeEnabled.HasValue) settings.AutoIndexFundTypeEnabled = request.AutoIndexFundTypeEnabled.Value;
        if (request.AutoClassificationEnabled.HasValue) settings.AutoClassificationEnabled = request.AutoClassificationEnabled.Value;
        settings.UpdatedAt = DateTime.UtcNow;
        settings.UpdatedBy = "admin"; // TODO: get from auth

        _syncSettingsRepo.Update(settings);
        await _fundRepo.SaveChangesAsync(cancellationToken); // persist settings via shared UoW

        return Ok(settings);
    }

    [HttpGet("sync/latest")]
    [ProducesResponseType(typeof(FundSyncLog), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLatestSyncRun(CancellationToken cancellationToken = default)
    {
        var run = await _syncService.GetLatestRunAsync(cancellationToken);
        if (run == null) return NotFound();
        return Ok(run);
    }

    [HttpGet("sync/history")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSyncHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _syncService.GetRunHistoryAsync(page, pageSize, cancellationToken);
        return Ok(new { items, totalCount, page, pageSize, totalPages = (int)Math.Ceiling((double)totalCount / pageSize) });
    }

    [HttpGet("sync/{id:int}/details")]
    [ProducesResponseType(typeof(List<FundSyncLogDetail>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSyncRunDetails(int id, CancellationToken cancellationToken = default)
    {
        var details = await _syncDetailRepo.GetByLogIdAsync(id, cancellationToken);
        return Ok(details);
    }

    [HttpPost("sync/run")]
    [ProducesResponseType(typeof(FundSyncLog), StatusCodes.Status200OK)]
    public async Task<IActionResult> RunSyncNow(CancellationToken cancellationToken = default)
    {
        try
        {
            var log = await _syncService.RunSyncAsync("Manual", cancellationToken);
            return Ok(log);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // ========== FUND CATEGORIES ==========

    // POST /api/admin/funds/logos/backfill
    /// <summary>
    /// Downloads any fund logo that is not yet on disk and fills LocalLogoPath
    /// only for files that really landed. Safe to re-run.
    /// </summary>
    [HttpPost("logos/backfill")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> BackfillLogos(CancellationToken cancellationToken = default)
    {
        var stored = await _syncService.BackfillLogosAsync(cancellationToken);
        return Ok(new { stored, message = $"Downloaded {stored} fund logo(s)." });
    }

    [HttpGet("categories")]
    [ProducesResponseType(typeof(List<FundCategory>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllCategories([FromQuery] bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var categories = await _categoryRepo.GetAllAsync(includeDeleted, cancellationToken);
        return Ok(categories);
    }

    [HttpGet("categories/{id:int}")]
    [ProducesResponseType(typeof(FundCategory), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategory(int id, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepo.GetByIdAsync(id, cancellationToken);
        if (category == null) return NotFound();
        return Ok(category);
    }

    [HttpPost("categories")]
    [ProducesResponseType(typeof(FundCategory), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = new FundCategory
        {
            NameAr = request.NameAr,
            NameEn = request.NameEn,
            Slug = request.Slug ?? GenerateSlug(request.NameEn ?? request.NameAr),
            Icon = request.Icon ?? string.Empty,
            Color = request.Color ?? string.Empty,
            IsAutoDerived = request.IsAutoDerived,
            IsIndexTracking = request.IsIndexTracking,
            AutoRuleJson = request.AutoRuleJson ?? string.Empty,
            IsVisible = request.IsVisible,
            DisplayOrder = request.DisplayOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = "admin",
            UpdatedBy = "admin"
        };

        await _categoryRepo.AddAsync(category, cancellationToken);
        await _fundRepo.SaveChangesAsync(cancellationToken);

        // Log change
        await LogCategoryChangeAsync(category.Id, null, CategoryChangeAction.CategoryCreated, $"Created category: {category.NameAr}", cancellationToken);

        return Ok(category);
    }

    [HttpPatch("categories/{id:int}")]
    [ProducesResponseType(typeof(FundCategory), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepo.GetByIdAsync(id, cancellationToken);
        if (category == null) return NotFound();

        var oldNameAr = category.NameAr;
        var oldSlug = category.Slug;

        category.NameAr = request.NameAr ?? category.NameAr;
        category.NameEn = request.NameEn ?? category.NameEn;
        if (request.Slug != null) category.Slug = request.Slug;
        category.Icon = request.Icon ?? category.Icon;
        category.Color = request.Color ?? category.Color;
        if (request.IsIndexTracking.HasValue) category.IsIndexTracking = request.IsIndexTracking.Value;
        category.IsVisible = request.IsVisible ?? category.IsVisible;
        category.DisplayOrder = request.DisplayOrder ?? category.DisplayOrder;
        if (request.AutoRuleJson != null) category.AutoRuleJson = request.AutoRuleJson;
        category.UpdatedAt = DateTime.UtcNow;
        category.UpdatedBy = "admin";

        _categoryRepo.Update(category);
        await _fundRepo.SaveChangesAsync(cancellationToken);

        // Log change
        if (oldNameAr != category.NameAr)
            await LogCategoryChangeAsync(category.Id, null, CategoryChangeAction.CategoryRenamed, $"Renamed from '{oldNameAr}' to '{category.NameAr}'", cancellationToken);

        return Ok(category);
    }

    [HttpDelete("categories/{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCategory(int id, [FromQuery] bool confirm = false, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepo.GetByIdAsync(id, cancellationToken);
        if (category == null) return NotFound();

        // Count funds in this category
        var overrides = await _overrideRepo.GetByCategoryIdAsync(id, cancellationToken);
        var fundCount = overrides.Count;

        if (!confirm && fundCount > 0)
        {
            return BadRequest(new { message = $"Category has {fundCount} funds. Pass confirm=true to delete.", fundCount });
        }

        // Soft delete
        category.IsDeleted = true;
        category.DeletedAt = DateTime.UtcNow;
        category.DeletedReason = $"Deleted by admin. Had {fundCount} funds.";
        _categoryRepo.Update(category);
        await _fundRepo.SaveChangesAsync(cancellationToken);

        // Remove all overrides for this category
        foreach (var o in overrides)
        {
            _overrideRepo.Delete(o);
        }
        await _fundRepo.SaveChangesAsync(cancellationToken);

        // Log change
        await LogCategoryChangeAsync(category.Id, null, CategoryChangeAction.CategoryDeleted, $"Deleted category: {category.NameAr} (had {fundCount} funds)", cancellationToken);

        return Ok(new { message = "Category deleted", fundCount });
    }

    [HttpPost("categories/{id:int}/restore")]
    [ProducesResponseType(typeof(FundCategory), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestoreCategory(int id, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepo.GetByIdAsync(id, cancellationToken);
        if (category == null) return NotFound();

        category.IsDeleted = false;
        category.DeletedAt = null;
        category.DeletedReason = string.Empty;
        _categoryRepo.Update(category);
        await _fundRepo.SaveChangesAsync(cancellationToken);

        await LogCategoryChangeAsync(category.Id, null, CategoryChangeAction.CategoryRestored, $"Restored category: {category.NameAr}", cancellationToken);

        return Ok(category);
    }

    [HttpPost("categories/merge")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> MergeCategories([FromBody] MergeCategoriesRequest request, CancellationToken cancellationToken = default)
    {
        var source = await _categoryRepo.GetByIdAsync(request.SourceCategoryId, cancellationToken);
        var target = await _categoryRepo.GetByIdAsync(request.TargetCategoryId, cancellationToken);
        if (source == null || target == null) return NotFound();

        // Move all overrides from source to target
        var overrides = await _overrideRepo.GetByCategoryIdAsync(source.Id, cancellationToken);
        foreach (var o in overrides)
        {
            var existing = await _overrideRepo.GetAsync(o.FundId, target.Id, cancellationToken);
            if (existing == null)
            {
                o.CategoryId = target.Id;
                _overrideRepo.Update(o);
            }
            else
            {
                // Merge logic: manual wins
                if (o.Action == OverrideAction.Include && existing.Action == OverrideAction.Exclude)
                {
                    existing.Action = OverrideAction.Include;
                    _overrideRepo.Update(existing);
                }
                _overrideRepo.Delete(o);
            }
        }

        await _fundRepo.SaveChangesAsync(cancellationToken);

        // Soft delete source
        source.IsDeleted = true;
        source.DeletedAt = DateTime.UtcNow;
        source.DeletedReason = $"Merged into '{target.NameAr}'";
        _categoryRepo.Update(source);
        await _fundRepo.SaveChangesAsync(cancellationToken);

        await LogCategoryChangeAsync(target.Id, null, CategoryChangeAction.CategoryMerged, $"Merged '{source.NameAr}' into '{target.NameAr}'", cancellationToken);

        return Ok(new { message = "Categories merged", movedOverrides = overrides.Count });
    }

    // ========== FUND CATEGORY OVERRIDES ==========

    [HttpGet("funds/{fundId:int}/categories")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFundCategories(int fundId, CancellationToken cancellationToken = default)
    {
        var fund = await _fundRepo.GetByIdAsync(fundId, cancellationToken);
        if (fund == null) return NotFound();

        var overrides = await _overrideRepo.GetByFundIdAsync(fundId, cancellationToken);
        var categories = await _categoryRepo.GetVisibleAsync(cancellationToken);

        var result = categories.Select(c => {
            var overrideEntry = overrides.FirstOrDefault(o => o.CategoryId == c.Id);
            return new
            {
                category = new { c.Id, c.NameAr, c.NameEn, c.Slug, c.IsAutoDerived },
                isIncluded = overrideEntry?.Action == OverrideAction.Include || (c.IsAutoDerived && overrideEntry == null), // simplified
                source = overrideEntry != null ? (overrideEntry.Action == OverrideAction.Include ? "Manual" : "Excluded") : (c.IsAutoDerived ? "Auto" : "None"),
                overrideId = overrideEntry?.Id
            };
        }).ToList();

        return Ok(result);
    }

    [HttpPost("funds/{fundId:int}/categories/{categoryId:int}/include")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> IncludeFundInCategory(int fundId, int categoryId, CancellationToken cancellationToken = default)
    {
        var existing = await _overrideRepo.GetAsync(fundId, categoryId, cancellationToken);
        if (existing != null)
        {
            if (existing.Action == OverrideAction.Exclude)
            {
                existing.Action = OverrideAction.Include;
                _overrideRepo.Update(existing);
            }
        }
        else
        {
            await _overrideRepo.AddAsync(new FundCategoryOverride
            {
                FundId = fundId,
                CategoryId = categoryId,
                Action = OverrideAction.Include,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "admin"
            }, cancellationToken);
        }
        await _fundRepo.SaveChangesAsync(cancellationToken);
        await LogCategoryChangeAsync(categoryId, fundId, CategoryChangeAction.ManualIncluded, "Manual include", cancellationToken);
        return Ok();
    }

    [HttpPost("funds/{fundId:int}/categories/{categoryId:int}/exclude")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExcludeFundFromCategory(int fundId, int categoryId, CancellationToken cancellationToken = default)
    {
        var existing = await _overrideRepo.GetAsync(fundId, categoryId, cancellationToken);
        if (existing != null)
        {
            if (existing.Action == OverrideAction.Include)
            {
                existing.Action = OverrideAction.Exclude;
                _overrideRepo.Update(existing);
            }
        }
        else
        {
            await _overrideRepo.AddAsync(new FundCategoryOverride
            {
                FundId = fundId,
                CategoryId = categoryId,
                Action = OverrideAction.Exclude,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "admin"
            }, cancellationToken);
        }
        await _fundRepo.SaveChangesAsync(cancellationToken);
        await LogCategoryChangeAsync(categoryId, fundId, CategoryChangeAction.ManualExcluded, "Manual exclude", cancellationToken);
        return Ok();
    }

    [HttpDelete("funds/{fundId:int}/categories/{categoryId:int}/override")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetFundCategoryOverride(int fundId, int categoryId, CancellationToken cancellationToken = default)
    {
        var existing = await _overrideRepo.GetAsync(fundId, categoryId, cancellationToken);
        if (existing != null)
        {
            _overrideRepo.Delete(existing);
            await _fundRepo.SaveChangesAsync(cancellationToken);
            await LogCategoryChangeAsync(categoryId, fundId, CategoryChangeAction.ResetToAuto, "Reset to auto", cancellationToken);
        }
        return Ok();
    }

    [HttpGet("funds/{fundId:int}/category-overrides")]
    [ProducesResponseType(typeof(List<FundCategoryOverride>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFundCategoryOverrides(int fundId, CancellationToken cancellationToken = default)
    {
        var overrides = await _overrideRepo.GetByFundIdAsync(fundId, cancellationToken);
        return Ok(overrides);
    }

    /// <summary>
    /// Funds currently in a category (final membership) with their source badge
    /// (Auto = derived by the sync, Manual = admin decision) and the
    /// auto-classification reason. Supports text search and an Auto-only filter.
    /// </summary>
    [HttpGet("categories/{id:int}/funds")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategoryFunds(
        int id,
        [FromQuery] string? search,
        [FromQuery] string? source,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepo.GetByIdAsync(id, cancellationToken);
        if (category == null) return NotFound();
        if (category.IsDeleted) return BadRequest("Category is deleted.");

        var funds = await _fundRepo.GetAllAsync(cancellationToken);
        var overrides = await _overrideRepo.GetByCategoryIdAsync(id, cancellationToken);
        var byFund = overrides.ToDictionary(o => o.FundId);

        var q = (search ?? string.Empty).Trim();
        var onlySource = (source ?? string.Empty).Trim();
        var members = new List<CategoryFundMemberDto>();
        foreach (var f in funds.OrderBy(f => f.Code))
        {
            if (!string.IsNullOrWhiteSpace(q) &&
                !f.Code.Contains(q, StringComparison.OrdinalIgnoreCase) &&
                !(f.NameAr ?? string.Empty).Contains(q, StringComparison.OrdinalIgnoreCase) &&
                !(f.NameEn ?? string.Empty).Contains(q, StringComparison.OrdinalIgnoreCase))
                continue;

            string? memberSource = null;
            string reason = string.Empty;
            if (byFund.TryGetValue(f.Id, out var o))
            {
                if (o.Action == OverrideAction.Exclude) continue;
                memberSource = o.CreatedBy == "AutoSync" ? "Auto" : "Manual";
                reason = o.CreatedBy == "AutoSync"
                    ? (Infrastructure.Services.FundAutoClassification.Evaluate(f, category.AutoRuleJson, out var r) ? r : string.Empty)
                    : "تعديل يدوي";
            }
            else if (category.IsAutoDerived)
            {
                if (!Infrastructure.Services.FundAutoClassification.Evaluate(f, category.AutoRuleJson, out var r))
                    continue;
                memberSource = "Auto";
                reason = r;
            }
            else
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(onlySource) &&
                !string.Equals(memberSource, onlySource, StringComparison.OrdinalIgnoreCase))
                continue;

            members.Add(new CategoryFundMemberDto(
                f.Id, f.Code, f.NameAr ?? string.Empty, f.NameEn ?? string.Empty,
                memberSource, reason));
        }

        var totalCount = members.Count;
        var items = members
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        return Ok(new { items, totalCount, page, pageSize, totalPages = (int)Math.Ceiling((double)totalCount / pageSize) });
    }

    /// <summary>
    /// Clears a fund's MANUAL overrides (Reset to auto). Auto-derived rows are
    /// left for the sync to recalculate.
    /// </summary>
    [HttpDelete("funds/{fundId:int}/overrides")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetFundOverrides(int fundId, CancellationToken cancellationToken = default)
    {
        var fund = await _fundRepo.GetByIdAsync(fundId, cancellationToken);
        if (fund == null) return NotFound();

        var manual = (await _overrideRepo.GetByFundIdAsync(fundId, cancellationToken))
            .Where(o => o.CreatedBy != "AutoSync")
            .ToList();
        foreach (var o in manual)
        {
            _overrideRepo.Delete(o);
            await LogCategoryChangeAsync(o.CategoryId, fundId, CategoryChangeAction.ResetToAuto, "Reset fund to auto", cancellationToken);
        }
        await _fundRepo.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Fund reset to auto", clearedCount = manual.Count });
    }

    /// <summary>
    /// Clears a category's MANUAL overrides (Reset to auto). Auto-derived rows
    /// are left for the sync to recalculate.
    /// </summary>
    [HttpDelete("categories/{id:int}/overrides")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetCategoryOverrides(int id, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepo.GetByIdAsync(id, cancellationToken);
        if (category == null) return NotFound();

        var manual = (await _overrideRepo.GetByCategoryIdAsync(id, cancellationToken))
            .Where(o => o.CreatedBy != "AutoSync")
            .ToList();
        foreach (var o in manual)
        {
            _overrideRepo.Delete(o);
            await LogCategoryChangeAsync(id, o.FundId, CategoryChangeAction.ResetToAuto, "Reset category to auto", cancellationToken);
        }
        await _fundRepo.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Category reset to auto", clearedCount = manual.Count });
    }

    // ========== CATEGORY CHANGE LOG ==========

    [HttpGet("category-changes")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
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
        var filter = new FundCategoryChangeLogFilter
        {
            FundId = fundId,
            CategoryId = categoryId,
            Action = action,
            FromDate = fromDate,
            ToDate = toDate,
            Page = page,
            PageSize = pageSize
        };
        var (items, totalCount) = await _changeLogRepo.QueryPagedAsync(filter, cancellationToken);
        return Ok(new { items, totalCount, page, pageSize, totalPages = (int)Math.Ceiling((double)totalCount / pageSize) });
    }

    // ========== FUND MANAGEMENT ==========

    [HttpGet("funds")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllFunds(
        [FromQuery] string? search,
        [FromQuery] string? type,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var filter = new FundListFilter
        {
            Search = search,
            Type = type,
            Page = page,
            PageSize = pageSize
        };
        var (items, totalCount) = await _fundRepo.QueryPagedAsync(filter, cancellationToken);
        return Ok(new { items, totalCount, page, pageSize, totalPages = (int)Math.Ceiling((double)totalCount / pageSize) });
    }

    [HttpGet("funds/{id:int}")]
    [ProducesResponseType(typeof(Fund), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFund(int id, CancellationToken cancellationToken = default)
    {
        var fund = await _fundRepo.GetByIdAsync(id, cancellationToken);
        if (fund == null) return NotFound();
        return Ok(fund);
    }

    private string GenerateSlug(string input)
    {
        return input.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("_", "-")
            .Replace(".", "")
            .Replace("/", "-");
    }

    private async Task LogCategoryChangeAsync(int categoryId, int? fundId, CategoryChangeAction action, string reason, CancellationToken cancellationToken)
    {
        await _changeLogRepo.AddAsync(new FundCategoryChangeLog
        {
            FundId = fundId,
            CategoryId = categoryId,
            Action = action,
            PerformedBy = "admin",
            Reason = reason,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);
        await _fundRepo.SaveChangesAsync(cancellationToken);
    }
}

// Request DTOs
public sealed class FundSyncSettingsUpdateRequest
{
    public bool? IsEnabled { get; set; }
    public TimeOnly? RunTimeCairo { get; set; }
    public bool? PreventOverlap { get; set; }
    public int? RequestDelaySeconds { get; set; }
    public int? MaxRetries { get; set; }
    public int? RetryBackoffSeconds { get; set; }
    public bool? AutoIndexFundTypeEnabled { get; set; }
    public bool? AutoClassificationEnabled { get; set; }
}

public sealed class CreateCategoryRequest
{
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public bool IsAutoDerived { get; set; }
    public bool IsIndexTracking { get; set; }
    public string? AutoRuleJson { get; set; }
    public bool IsVisible { get; set; } = true;
    public int DisplayOrder { get; set; }
}

public sealed class UpdateCategoryRequest
{
    public string? NameAr { get; set; }
    public string? NameEn { get; set; }
    public string? Slug { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public bool? IsIndexTracking { get; set; }
    public bool? IsVisible { get; set; }
    public int? DisplayOrder { get; set; }
    public string? AutoRuleJson { get; set; }
}

public sealed class MergeCategoriesRequest
{
    public int SourceCategoryId { get; set; }
    public int TargetCategoryId { get; set; }
}

public sealed record CategoryFundMemberDto(
    int Id,
    string Code,
    string NameAr,
    string NameEn,
    string Source,
    string Reason);