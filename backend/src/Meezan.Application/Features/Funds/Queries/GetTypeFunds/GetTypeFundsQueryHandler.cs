using MediatR;
using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Entities;
using Meezan.Domain.Services;

namespace Meezan.Application.Features.Funds.Queries.GetTypeFunds;

public sealed class GetTypeFundsQueryHandler : IRequestHandler<GetTypeFundsQuery, TypeFundsResultDto?>
{
    private readonly IFundTypeDefinitionRepository _typeDefRepo;
    private readonly IFundTypeOverrideRepository _typeOverrideRepo;
    private readonly IFundRepository _fundRepo;
    private readonly IFundCategoryRepository _categoryRepo;
    private readonly IFundCategoryOverrideRepository _categoryOverrideRepo;
    private readonly IFundSyncSettingsRepository _syncSettingsRepo;

    public GetTypeFundsQueryHandler(
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

    public async Task<TypeFundsResultDto?> Handle(GetTypeFundsQuery request, CancellationToken cancellationToken)
    {
        var targetType = await _typeDefRepo.GetByIdAsync(request.Id, cancellationToken);
        if (targetType == null) return null;

        var allTypes = await _typeDefRepo.GetAllAsync(includeDeleted: false, cancellationToken);
        var defsById = allTypes.ToDictionary(t => t.Id);
        var defsBySlug = allTypes.ToDictionary(t => t.Slug);

        var overrides = await _typeOverrideRepo.GetAllFundOverridesAsync(cancellationToken);
        var syncSettings = await _syncSettingsRepo.GetAsync(cancellationToken);
        var autoIndexEnabled = syncSettings?.AutoIndexFundTypeEnabled ?? true;

        var allCategories = await _categoryRepo.GetAllAsync(includeDeleted: false, cancellationToken);
        var indexTrackingCategories = allCategories.Where(c => c.IsIndexTracking).ToList();
        var indexTrackingCatIds = indexTrackingCategories.Select(c => c.Id).ToHashSet();

        var allCategoryOverrides = new List<FundCategoryOverride>();
        foreach (var cat in allCategories)
        {
            var catOverrides = await _categoryOverrideRepo.GetByCategoryIdAsync(cat.Id, cancellationToken);
            allCategoryOverrides.AddRange(catOverrides);
        }
        var categoryOverridesByFund = allCategoryOverrides.GroupBy(o => o.FundId).ToDictionary(g => g.Key, g => g.ToList());

        var indexTrackingFundIds = new HashSet<int>();
        var allFunds = await _fundRepo.GetAllAsync(cancellationToken);
        var activeFunds = allFunds.Where(f => f.IsActive).ToList();

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

        var matchingFunds = new List<AdminTypeFundRowDto>();
        var searchLower = (request.Search ?? string.Empty).Trim().ToLowerInvariant();

        foreach (var fund in activeFunds)
        {
            var eff = FundEffectiveTypeEngine.Calculate(
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
                    if (FundAutoClassification.Evaluate(fund, cat.AutoRuleJson, out _))
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
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return new TypeFundsResultDto(
            pagedItems,
            totalCount,
            request.Page,
            request.PageSize,
            (int)Math.Ceiling((double)totalCount / request.PageSize)
        );
    }
}
