using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Entities;
using Meezan.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Meezan.WebApi.Controllers;

/// <summary>
/// Editable fund types (الأنواع). Distinct from /categories: a type is the
/// SNDUK classification of the fund itself (equity, fixed income, ...), with
/// operator-editable AR/EN labels, ordering and visibility.
/// </summary>
[ApiController]
[Authorize]
[Route("api/admin/funds/types")]
public class AdminFundTypesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AdminFundTypesController(ApplicationDbContext db)
    {
        _db = db;
    }

    private async Task LogAsync(int? fundId, int? typeId, FundTypeChangeAction action, string reason, CancellationToken ct)
    {
        _db.FundTypeChangeLogs.Add(new FundTypeChangeLog
        {
            FundId = fundId,
            FundTypeDefinitionId = typeId,
            Action = action,
            PerformedBy = "admin",
            Reason = reason,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
    }

    // GET /api/admin/funds/types
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeDeleted = false, CancellationToken ct = default)
    {
        var q = _db.FundTypeDefinitions.AsNoTracking();
        if (!includeDeleted) q = q.Where(t => !t.IsDeleted);
        var items = await q.OrderBy(t => t.DisplayOrder).ThenBy(t => t.NameAr).ToListAsync(ct);

        // Preload effective type state for all active funds to compute exact counts per type
        var activeFunds = await _db.Funds.AsNoTracking().Where(f => f.IsActive).ToListAsync(ct);
        var defsById = items.ToDictionary(t => t.Id);
        var defsBySlug = items.ToDictionary(t => t.Slug);
        var overrides = await _db.FundTypeOverrides.AsNoTracking().ToDictionaryAsync(o => o.FundId, o => o.FundTypeDefinitionId, ct);
        var syncSettings = await _db.FundSyncSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        var autoIndexEnabled = syncSettings?.AutoIndexFundTypeEnabled ?? true;

        var indexTrackingCategories = await _db.FundCategories.AsNoTracking()
            .Where(c => c.IsIndexTracking && !c.IsDeleted)
            .ToListAsync(ct);
        var indexTrackingCatIds = indexTrackingCategories.Select(c => c.Id).ToHashSet();
        var categoryOverrides = await _db.FundCategoryOverrides.AsNoTracking()
            .Where(o => indexTrackingCatIds.Contains(o.CategoryId))
            .ToListAsync(ct);

        var indexTrackingFundIds = new HashSet<int>();
        foreach (var fund in activeFunds)
        {
            var isMember = false;
            foreach (var cat in indexTrackingCategories)
            {
                var ov = categoryOverrides.FirstOrDefault(o => o.CategoryId == cat.Id && o.FundId == fund.Id);
                if (ov != null)
                {
                    if (ov.Action == OverrideAction.Include) { isMember = true; break; }
                }
                else if (cat.IsAutoDerived)
                {
                    if (Infrastructure.Services.FundAutoClassification.Evaluate(fund, cat.AutoRuleJson, out _))
                    {
                        isMember = true;
                        break;
                    }
                }
            }
            if (isMember) indexTrackingFundIds.Add(fund.Id);
        }

        var countsBySlug = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var fund in activeFunds)
        {
            var eff = Infrastructure.Services.FundEffectiveTypeEngine.Calculate(
                fund, defsById, defsBySlug, overrides, autoIndexEnabled, indexTrackingFundIds);
            countsBySlug[eff.Slug] = countsBySlug.GetValueOrDefault(eff.Slug) + 1;
        }

        var result = items.Select(t => new
        {
            t.Id,
            t.Slug,
            t.NameAr,
            t.NameEn,
            t.Icon,
            t.Color,
            t.DisplayOrder,
            t.IsVisible,
            t.IsDeleted,
            t.DeletedAt,
            t.DeletedReason,
            t.CreatedAt,
            t.UpdatedAt,
            t.CreatedBy,
            t.UpdatedBy,
            FundCount = countsBySlug.GetValueOrDefault(t.Slug, 0)
        });

        return Ok(result);
    }

    // GET /api/admin/funds/types/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct = default)
    {
        var t = await _db.FundTypeDefinitions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return t == null ? NotFound() : Ok(t);
    }

    // POST /api/admin/funds/types
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFundTypeRequest req, CancellationToken ct = default)
    {
        var slug = string.IsNullOrWhiteSpace(req.Slug)
            ? GenerateSlug(req.NameEn ?? req.NameAr)
            : req.Slug.Trim().ToLowerInvariant();

        if (await _db.FundTypeDefinitions.AnyAsync(t => t.Slug == slug, ct))
            return BadRequest(new { message = $"Slug '{slug}' already exists." });

        var now = DateTime.UtcNow;
        var entity = new FundTypeDefinition
        {
            Slug = slug,
            NameAr = req.NameAr,
            NameEn = req.NameEn,
            Icon = req.Icon ?? string.Empty,
            Color = req.Color ?? string.Empty,
            DisplayOrder = req.DisplayOrder,
            IsVisible = req.IsVisible,
            IsDeleted = false,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "admin",
            UpdatedBy = "admin"
        };
        _db.FundTypeDefinitions.Add(entity);
        await _db.SaveChangesAsync(ct);
        await LogAsync(null, entity.Id, FundTypeChangeAction.Created, $"Created type '{entity.NameAr}'", ct);
        return Ok(entity);
    }

    // PATCH /api/admin/funds/types/{id}
    [HttpPatch("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateFundTypeRequest req, CancellationToken ct = default)
    {
        var t = await _db.FundTypeDefinitions.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t == null) return NotFound();

        var oldNameAr = t.NameAr;
        var oldVisible = t.IsVisible;
        var oldOrder = t.DisplayOrder;

        if (req.NameAr != null) t.NameAr = req.NameAr;
        if (req.NameEn != null) t.NameEn = req.NameEn;
        if (req.Icon != null) t.Icon = req.Icon;
        if (req.Color != null) t.Color = req.Color;
        if (req.DisplayOrder.HasValue) t.DisplayOrder = req.DisplayOrder.Value;
        if (req.IsVisible.HasValue) t.IsVisible = req.IsVisible.Value;
        t.UpdatedAt = DateTime.UtcNow;
        t.UpdatedBy = "admin";

        await _db.SaveChangesAsync(ct);

        if (oldNameAr != t.NameAr)
            await LogAsync(null, t.Id, FundTypeChangeAction.Renamed, $"Renamed '{oldNameAr}' → '{t.NameAr}'", ct);
        if (oldVisible != t.IsVisible)
            await LogAsync(null, t.Id, FundTypeChangeAction.VisibilityChanged, t.IsVisible ? "Shown" : "Hidden", ct);
        if (oldOrder != t.DisplayOrder)
            await LogAsync(null, t.Id, FundTypeChangeAction.Reordered, $"{oldOrder} → {t.DisplayOrder}", ct);

        return Ok(t);
    }

    // DELETE /api/admin/funds/types/{id}?confirm=true
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] bool confirm = false, CancellationToken ct = default)
    {
        var t = await _db.FundTypeDefinitions.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t == null) return NotFound();

        var fundCount = await _db.FundTypeOverrides.CountAsync(o => o.FundTypeDefinitionId == id, ct);
        fundCount += await _db.Funds.CountAsync(f => f.Type == Infrastructure.Services.FundTypeCatalog.EnumFor(t.Slug), ct);

        if (!confirm && fundCount > 0)
        {
            return BadRequest(new { message = $"Type has {fundCount} fund(s). Pass confirm=true to delete.", fundCount });
        }

        t.IsDeleted = true;
        t.DeletedAt = DateTime.UtcNow;
        t.DeletedReason = $"Deleted by admin. Had {fundCount} fund(s).";
        t.IsVisible = false;
        t.UpdatedAt = DateTime.UtcNow;
        t.UpdatedBy = "admin";
        await _db.SaveChangesAsync(ct);

        // Drop the manual overrides that pointed at the removed type.
        var overrides = await _db.FundTypeOverrides.Where(o => o.FundTypeDefinitionId == id).ToListAsync(ct);
        _db.FundTypeOverrides.RemoveRange(overrides);
        await _db.SaveChangesAsync(ct);

        await LogAsync(null, t.Id, FundTypeChangeAction.Deleted, $"Deleted type '{t.NameAr}' (had {fundCount} fund(s))", ct);
        return Ok(new { message = "Type deleted", fundCount });
    }

    // POST /api/admin/funds/types/{id}/restore
    [HttpPost("{id:int}/restore")]
    public async Task<IActionResult> Restore(int id, CancellationToken ct = default)
    {
        var t = await _db.FundTypeDefinitions.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t == null) return NotFound();

        t.IsDeleted = false;
        t.DeletedAt = null;
        t.DeletedReason = string.Empty;
        t.IsVisible = true;
        t.UpdatedAt = DateTime.UtcNow;
        t.UpdatedBy = "admin";
        await _db.SaveChangesAsync(ct);
        await LogAsync(null, t.Id, FundTypeChangeAction.Restored, $"Restored type '{t.NameAr}'", ct);
        return Ok(t);
    }

    // POST /api/admin/funds/types/merge
    [HttpPost("merge")]
    public async Task<IActionResult> Merge([FromBody] MergeFundTypesRequest req, CancellationToken ct = default)
    {
        var source = await _db.FundTypeDefinitions.FirstOrDefaultAsync(x => x.Id == req.SourceId, ct);
        var target = await _db.FundTypeDefinitions.FirstOrDefaultAsync(x => x.Id == req.TargetId, ct);
        if (source == null || target == null) return NotFound();

        var moved = await _db.FundTypeOverrides.Where(o => o.FundTypeDefinitionId == source.Id).ToListAsync(ct);
        foreach (var o in moved)
        {
            o.FundTypeDefinitionId = target.Id;
            o.CreatedBy = "admin";
        }
        await _db.SaveChangesAsync(ct);

        source.IsDeleted = true;
        source.DeletedAt = DateTime.UtcNow;
        source.IsVisible = false;
        source.DeletedReason = $"Merged into '{target.NameAr}'";
        source.UpdatedAt = DateTime.UtcNow;
        source.UpdatedBy = "admin";
        await _db.SaveChangesAsync(ct);

        await LogAsync(null, target.Id, FundTypeChangeAction.Merged, $"Merged '{source.NameAr}' into '{target.NameAr}' ({moved.Count} overrides moved)", ct);
        return Ok(new { message = "Types merged", movedOverrides = moved.Count });
    }

    // POST /api/admin/funds/types/reorder
    [HttpPost("reorder")]
    public async Task<IActionResult> Reorder([FromBody] ReorderFundTypesRequest req, CancellationToken ct = default)
    {
        var ids = req.OrderedIds ?? new List<int>();
        var all = await _db.FundTypeDefinitions.Where(t => ids.Contains(t.Id)).ToListAsync(ct);
        var index = 0;
        foreach (var id in ids)
        {
            var t = all.FirstOrDefault(x => x.Id == id);
            if (t == null) continue;
            t.DisplayOrder = ++index;
            t.UpdatedAt = DateTime.UtcNow;
            t.UpdatedBy = "admin";
        }
        await _db.SaveChangesAsync(ct);
        await LogAsync(null, null, FundTypeChangeAction.Reordered, $"Reordered {ids.Count} type(s)", ct);
        return Ok(new { reordered = ids.Count });
    }

    // GET /api/admin/funds/types/{id}/funds
    [HttpGet("{id:int}/funds")]
    public async Task<IActionResult> GetTypeFunds(
        int id,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var targetType = await _db.FundTypeDefinitions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (targetType == null) return NotFound();

        var allTypes = await _db.FundTypeDefinitions.AsNoTracking().Where(t => !t.IsDeleted).ToListAsync(ct);
        var defsById = allTypes.ToDictionary(t => t.Id);
        var defsBySlug = allTypes.ToDictionary(t => t.Slug);

        var overrides = await _db.FundTypeOverrides.AsNoTracking().ToDictionaryAsync(o => o.FundId, o => o.FundTypeDefinitionId, ct);
        var syncSettings = await _db.FundSyncSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        var autoIndexEnabled = syncSettings?.AutoIndexFundTypeEnabled ?? true;

        var allCategories = await _db.FundCategories.AsNoTracking().Where(c => !c.IsDeleted).ToListAsync(ct);
        var indexTrackingCategories = allCategories.Where(c => c.IsIndexTracking).ToList();
        var indexTrackingCatIds = indexTrackingCategories.Select(c => c.Id).ToHashSet();

        var categoryOverrides = await _db.FundCategoryOverrides.AsNoTracking().ToListAsync(ct);
        var categoryOverridesByFund = categoryOverrides.GroupBy(o => o.FundId).ToDictionary(g => g.Key, g => g.ToList());

        var indexTrackingFundIds = new HashSet<int>();
        var activeFunds = await _db.Funds.AsNoTracking().Where(f => f.IsActive).ToListAsync(ct);

        foreach (var fund in activeFunds)
        {
            var isMember = false;
            foreach (var cat in indexTrackingCategories)
            {
                var ov = categoryOverrides.FirstOrDefault(o => o.CategoryId == cat.Id && o.FundId == fund.Id);
                if (ov != null)
                {
                    if (ov.Action == OverrideAction.Include) { isMember = true; break; }
                }
                else if (cat.IsAutoDerived)
                {
                    if (Infrastructure.Services.FundAutoClassification.Evaluate(fund, cat.AutoRuleJson, out _))
                    {
                        isMember = true;
                        break;
                    }
                }
            }
            if (isMember) indexTrackingFundIds.Add(fund.Id);
        }

        var matchingFunds = new List<AdminTypeFundRowDto>();
        var searchLower = (search ?? string.Empty).Trim().ToLowerInvariant();

        foreach (var fund in activeFunds)
        {
            var eff = Infrastructure.Services.FundEffectiveTypeEngine.Calculate(
                fund, defsById, defsBySlug, overrides, autoIndexEnabled, indexTrackingFundIds);

            if (eff.Slug != targetType.Slug && eff.TypeId != targetType.Id)
                continue;

            if (!string.IsNullOrWhiteSpace(searchLower))
            {
                var matches = fund.Code.ToLowerInvariant().Contains(searchLower) ||
                              (fund.NameAr ?? string.Empty).ToLowerInvariant().Contains(searchLower) ||
                              (fund.NameEn ?? string.Empty).ToLowerInvariant().Contains(searchLower) ||
                              (fund.AssetManagerName ?? string.Empty).ToLowerInvariant().Contains(searchLower);
                if (!matches) continue;
            }

            // Determine fund categories for chips
            var fundCats = new List<FundCategoryChipDto>();
            categoryOverridesByFund.TryGetValue(fund.Id, out var fOverrides);
            fOverrides ??= [];

            foreach (var cat in allCategories)
            {
                var ov = fOverrides.FirstOrDefault(o => o.CategoryId == cat.Id);
                if (ov != null)
                {
                    if (ov.Action == OverrideAction.Include)
                        fundCats.Add(new FundCategoryChipDto(cat.Id, cat.Slug, cat.NameAr));
                }
                else if (cat.IsAutoDerived)
                {
                    if (Infrastructure.Services.FundAutoClassification.Evaluate(fund, cat.AutoRuleJson, out _))
                        fundCats.Add(new FundCategoryChipDto(cat.Id, cat.Slug, cat.NameAr));
                }
            }

            overrides.TryGetValue(fund.Id, out var manualOverrideDefId);

            matchingFunds.Add(new AdminTypeFundRowDto(
                fund.Id,
                fund.Code,
                fund.NameAr,
                fund.NameEn,
                fund.AssetManagerName ?? string.Empty,
                eff.TypeId,
                eff.Slug,
                eff.NameAr,
                eff.Source,
                eff.OriginalSndukSlug,
                eff.OriginalSndukNameAr,
                manualOverrideDefId > 0 ? manualOverrideDefId : null,
                fundCats
            ));
        }

        var totalCount = matchingFunds.Count;
        var pagedItems = matchingFunds
            .OrderBy(x => x.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Ok(new
        {
            items = pagedItems,
            totalCount,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        });
    }

    // POST /api/admin/funds/types/funds/{fundId}/{typeId}
    [HttpPost("funds/{fundId:int}/{typeId:int}")]
    public async Task<IActionResult> SetFundType(int fundId, int typeId, CancellationToken ct = default)
    {
        var fund = await _db.Funds.FirstOrDefaultAsync(f => f.Id == fundId, ct);
        if (fund == null) return NotFound();
        var t = await _db.FundTypeDefinitions.FirstOrDefaultAsync(x => x.Id == typeId, ct);
        if (t == null) return NotFound();

        var existing = await _db.FundTypeOverrides.FirstOrDefaultAsync(o => o.FundId == fundId, ct);
        if (existing == null)
        {
            _db.FundTypeOverrides.Add(new FundTypeOverride
            {
                FundId = fundId,
                FundTypeDefinitionId = typeId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "admin"
            });
        }
        else
        {
            existing.FundTypeDefinitionId = typeId;
            existing.CreatedAt = DateTime.UtcNow;
            existing.CreatedBy = "admin";
        }
        await _db.SaveChangesAsync(ct);
        await LogAsync(fundId, typeId, FundTypeChangeAction.FundOverridden, $"Fund {fund.Code} → '{t.NameAr}'", ct);
        return Ok(new { message = "Fund type overridden", fundId, typeId });
    }

    // POST /api/admin/funds/types/{typeId}/funds/bulk
    [HttpPost("{typeId:int}/funds/bulk")]
    public async Task<IActionResult> BulkAddFundsToType(int typeId, [FromBody] BulkTypeFundRequest req, CancellationToken ct = default)
    {
        var targetType = await _db.FundTypeDefinitions.FirstOrDefaultAsync(x => x.Id == typeId, ct);
        if (targetType == null) return NotFound();

        var fundIds = req.FundIds ?? new List<int>();
        var activeFunds = await _db.Funds.Where(f => fundIds.Contains(f.Id) && f.IsActive).ToListAsync(ct);
        var activeFundIds = activeFunds.Select(f => f.Id).ToHashSet();
        var failed = fundIds.Where(id => !activeFundIds.Contains(id)).ToList();

        using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var existingOverrides = await _db.FundTypeOverrides.Where(o => activeFundIds.Contains(o.FundId)).ToListAsync(ct);
            var existingByFund = existingOverrides.ToDictionary(o => o.FundId);

            foreach (var fund in activeFunds)
            {
                if (existingByFund.TryGetValue(fund.Id, out var existing))
                {
                    existing.FundTypeDefinitionId = targetType.Id;
                    existing.CreatedAt = DateTime.UtcNow;
                    existing.CreatedBy = "admin";
                }
                else
                {
                    _db.FundTypeOverrides.Add(new FundTypeOverride
                    {
                        FundId = fund.Id,
                        FundTypeDefinitionId = targetType.Id,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = "admin"
                    });
                }

                _db.FundTypeChangeLogs.Add(new FundTypeChangeLog
                {
                    FundId = fund.Id,
                    FundTypeDefinitionId = targetType.Id,
                    Action = FundTypeChangeAction.FundOverridden,
                    PerformedBy = "admin",
                    Reason = $"Bulk added to type '{targetType.NameAr}'",
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return Ok(new { added = activeFunds.Count, failed });
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    // POST /api/admin/funds/types/funds/{fundId}/move
    [HttpPost("funds/{fundId:int}/move")]
    public async Task<IActionResult> MoveFundType(int fundId, [FromBody] MoveFundTypeRequest req, CancellationToken ct = default)
    {
        var fund = await _db.Funds.FirstOrDefaultAsync(f => f.Id == fundId, ct);
        if (fund == null) return NotFound();

        var existing = await _db.FundTypeOverrides.FirstOrDefaultAsync(o => o.FundId == fundId, ct);

        if (req.TargetTypeId.HasValue)
        {
            var targetType = await _db.FundTypeDefinitions.FirstOrDefaultAsync(t => t.Id == req.TargetTypeId.Value, ct);
            if (targetType == null) return BadRequest("Target type not found");

            if (existing == null)
            {
                _db.FundTypeOverrides.Add(new FundTypeOverride
                {
                    FundId = fundId,
                    FundTypeDefinitionId = targetType.Id,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "admin"
                });
            }
            else
            {
                existing.FundTypeDefinitionId = targetType.Id;
                existing.CreatedAt = DateTime.UtcNow;
                existing.CreatedBy = "admin";
            }
            await _db.SaveChangesAsync(ct);
            await LogAsync(fundId, targetType.Id, FundTypeChangeAction.FundOverridden, $"Moved fund {fund.Code} to '{targetType.NameAr}'", ct);
            return Ok(new { message = $"Moved to '{targetType.NameAr}'", fundId, targetTypeId = targetType.Id });
        }
        else
        {
            // Reset to auto
            if (existing != null)
            {
                _db.FundTypeOverrides.Remove(existing);
                await _db.SaveChangesAsync(ct);
            }
            await LogAsync(fundId, existing?.FundTypeDefinitionId, FundTypeChangeAction.FundOverrideRemoved, $"Fund {fund.Code} reset to default type", ct);
            return Ok(new { message = "Fund reset to default type", fundId, cleared = existing != null });
        }
    }

    // DELETE /api/admin/funds/types/funds/{fundId}
    [HttpDelete("funds/{fundId:int}")]
    public async Task<IActionResult> ClearFundType(int fundId, CancellationToken ct = default)
    {
        var fund = await _db.Funds.AsNoTracking().FirstOrDefaultAsync(f => f.Id == fundId, ct);
        if (fund == null) return NotFound();

        var existing = await _db.FundTypeOverrides.FirstOrDefaultAsync(o => o.FundId == fundId, ct);
        if (existing != null)
        {
            _db.FundTypeOverrides.Remove(existing);
            await _db.SaveChangesAsync(ct);
        }
        await LogAsync(fundId, existing?.FundTypeDefinitionId, FundTypeChangeAction.FundOverrideRemoved, $"Fund {fund.Code} reset to SNDUK type", ct);
        return Ok(new { message = "Fund type reset to auto", cleared = existing != null });
    }

    // GET /api/admin/funds/types/changes
    [HttpGet("changes")]
    public async Task<IActionResult> GetChanges([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var q = _db.FundTypeChangeLogs
            .Include(l => l.Fund)
            .Include(l => l.Definition)
            .OrderByDescending(l => l.CreatedAt)
            .AsNoTracking();

        var total = await q.CountAsync(ct);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(new { items, totalCount = total, page, pageSize, totalPages = (int)Math.Ceiling(total / (double)pageSize) });
    }

    private static string GenerateSlug(string input)
    {
        var s = input.ToLowerInvariant().Replace(" ", "-").Replace("_", "-");
        var sb = new System.Text.StringBuilder();
        foreach (var ch in s)
            if (char.IsLetterOrDigit(ch) || ch == '-') sb.Append(ch);
        return sb.ToString().Trim('-');
    }
}

public sealed record FundCategoryChipDto(int Id, string Slug, string NameAr);

public sealed record AdminTypeFundRowDto(
    int Id,
    string Code,
    string NameAr,
    string NameEn,
    string AssetManagerName,
    int? EffectiveTypeId,
    string EffectiveTypeSlug,
    string EffectiveTypeNameAr,
    string Source, // "SNDUK" | "يدوي" | "تلقائي - يتبع مؤشر"
    string? OriginalSndukSlug,
    string? OriginalSndukNameAr,
    int? OverrideTypeId,
    List<FundCategoryChipDto> Categories
);

public sealed class BulkTypeFundRequest
{
    public List<int>? FundIds { get; set; }
}

public sealed class MoveFundTypeRequest
{
    public int? TargetTypeId { get; set; }
}

public sealed class CreateFundTypeRequest
{
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsVisible { get; set; } = true;
}

public sealed class UpdateFundTypeRequest
{
    public string? NameAr { get; set; }
    public string? NameEn { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public int? DisplayOrder { get; set; }
    public bool? IsVisible { get; set; }
}

public sealed class MergeFundTypesRequest
{
    public int SourceId { get; set; }
    public int TargetId { get; set; }
}

public sealed class ReorderFundTypesRequest
{
    public List<int>? OrderedIds { get; set; }
}