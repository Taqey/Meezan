using MediatR;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Results;
using Meezan.Domain.Entities;
using Meezan.Domain.Services;

namespace Meezan.Application.Features.Funds.Types.Queries.GetAdminFundTypes;

public sealed class GetAdminFundTypesQueryHandler : IRequestHandler<GetAdminFundTypesQuery, Result<List<AdminFundTypeItemDto>>>
{
    private readonly IFundTypeDefinitionRepository _typeDefRepo;
    private readonly IFundTypeOverrideRepository _typeOverrideRepo;
    private readonly IFundRepository _fundRepo;
    private readonly IFundCategoryRepository _categoryRepo;
    private readonly IFundCategoryOverrideRepository _categoryOverrideRepo;
    private readonly IFundSyncSettingsRepository _syncSettingsRepo;

    public GetAdminFundTypesQueryHandler(
        IFundTypeDefinitionRepository typeDefRepo,
        IFundTypeOverrideRepository typeOverrideRepo,
        IFundRepository fundRepo,
        IFundCategoryRepository categoryRepo,
        IFundCategoryOverrideRepository categoryOverrideRepo,
        IFundSyncSettingsRepository syncSettingsRepo)
    {
        _typeDefRepo = typeDefRepo;
        _typeOverrideRepo = typeOverrideRepo;
        _fundRepo = fundRepo;
        _categoryRepo = categoryRepo;
        _categoryOverrideRepo = categoryOverrideRepo;
        _syncSettingsRepo = syncSettingsRepo;
    }

    public async Task<Result<List<AdminFundTypeItemDto>>> Handle(GetAdminFundTypesQuery request, CancellationToken cancellationToken)
    {
        var items = await _typeDefRepo.GetAllAsync(request.IncludeDeleted, cancellationToken);
        var allFunds = await _fundRepo.GetAllAsync(cancellationToken);
        var activeFunds = allFunds.Where(f => f.IsActive).ToList();

        var defsById = items.ToDictionary(t => t.Id);
        var defsBySlug = items.ToDictionary(t => t.Slug);
        var overrides = await _typeOverrideRepo.GetAllFundOverridesAsync(cancellationToken);
        var syncSettings = await _syncSettingsRepo.GetAsync(cancellationToken);
        var autoIndexEnabled = syncSettings?.AutoIndexFundTypeEnabled ?? true;

        var allCategories = await _categoryRepo.GetAllAsync(includeDeleted: false, cancellationToken);
        var indexTrackingCategories = allCategories.Where(c => c.IsIndexTracking).ToList();
        var indexTrackingCatIds = indexTrackingCategories.Select(c => c.Id).ToHashSet();

        var allCategoryOverrides = new List<FundCategoryOverride>();
        foreach (var catId in indexTrackingCatIds)
        {
            var catOverrides = await _categoryOverrideRepo.GetByCategoryIdAsync(catId, cancellationToken);
            allCategoryOverrides.AddRange(catOverrides);
        }

        var indexTrackingFundIds = new HashSet<int>();
        foreach (var fund in activeFunds)
        {
            var isMember = false;
            foreach (var cat in indexTrackingCategories)
            {
                var ov = allCategoryOverrides.FirstOrDefault(o => o.CategoryId == cat.Id && o.FundId == fund.Id);
                if (ov != null)
                {
                    if (ov.Action == OverrideAction.Include) { isMember = true; break; }
                }
                else if (cat.IsAutoDerived)
                {
                    if (FundAutoClassification.Evaluate(fund, cat.AutoRuleJson, out _))
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
            var eff = FundEffectiveTypeEngine.Calculate(
                fund, defsById, defsBySlug, overrides, autoIndexEnabled, indexTrackingFundIds);
            countsBySlug[eff.Slug] = countsBySlug.GetValueOrDefault(eff.Slug) + 1;
        }

        return items.Select(t => new AdminFundTypeItemDto(
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
            countsBySlug.GetValueOrDefault(t.Slug, 0)
        )).ToList();
    }
}
