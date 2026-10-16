using MediatR;
using Meezan.Application.Common.Interfaces;
using Meezan.Application.Features.Funds.Common;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Queries.GetFundById;

public sealed class GetFundByIdQueryHandler : IRequestHandler<GetFundByIdQuery, FundDetailDto?>
{
    private readonly IFundDetailQuery _detailQuery;

    public GetFundByIdQueryHandler(IFundDetailQuery detailQuery)
    {
        _detailQuery = detailQuery;
    }

    public async Task<FundDetailDto?> Handle(GetFundByIdQuery request, CancellationToken cancellationToken)
    {
        var bundle = await _detailQuery.LoadAsync(request.Id, cancellationToken);
        if (bundle?.Fund == null) return null;
        var fund = bundle.Fund;

        var priceHistory = bundle.PriceHistory;
        var analysis = bundle.Analysis;
        var similarFunds = bundle.SimilarFunds;
        var overrides = bundle.Overrides;
        var categories = bundle.Categories;

        var categoryMemberships = new List<FundCategoryMembershipDto>();
        foreach (var cat in categories)
        {
            var isIncluded = false;
            var source = "None";

            var overrideEntry = overrides.FirstOrDefault(o => o.CategoryId == cat.Id);
            if (overrideEntry != null)
            {
                isIncluded = overrideEntry.Action == OverrideAction.Include;
                source = overrideEntry.Action == OverrideAction.Include
                    ? (overrideEntry.CreatedBy == "AutoSync" ? "Auto" : "Manual")
                    : "Excluded";
            }
            else if (cat.IsAutoDerived)
            {
                source = "Auto";
            }

            if (isIncluded)
            {
                categoryMemberships.Add(new FundCategoryMembershipDto
                {
                    CategoryId = cat.Id,
                    CategoryNameAr = cat.NameAr,
                    CategoryNameEn = cat.NameEn,
                    CategorySlug = cat.Slug,
                    Source = source
                });
            }
        }

        var defsById = bundle.TypeDefinitions.ToDictionary(t => t.Id);
        var detailOverrides = bundle.TypeOverrides
            .Where(o => defsById.ContainsKey(o.FundTypeDefinitionId))
            .ToDictionary(o => o.FundId, o => (o.FundTypeDefinitionId, defsById[o.FundTypeDefinitionId].Slug));
        var detailDefs = defsById.Values
            .ToDictionary(d => d.Slug, d => d);

        var result = new FundDetailDto
        {
            Id = fund.Id,
            SndukId = fund.SndukId,
            PublicId = fund.PublicId,
            Slug = fund.Slug,
            NameAr = fund.NameAr,
            NameEn = fund.NameEn,
            Code = fund.Code,
            Type = fund.Type,
            TypeNameAr = fund.TypeNameAr,
            TypeNameEn = fund.TypeNameEn,
            RiskLevel = fund.RiskLevel,
            IsIslamic = fund.IsIslamic,
            IsFeatured = fund.IsFeatured,
            CurrentPrice = fund.CurrentPrice,
            DailyChange = fund.DailyChange,
            Return1M = fund.Return1M,
            Return3M = fund.Return3M,
            Return6M = fund.Return6M,
            Return1Y = fund.Return1Y,
            Return3Y = fund.Return3Y,
            TotalScore = fund.TotalScore,
            Aum = fund.Aum,
            ManagementFee = fund.ManagementFee,
            MinimumInvestment = fund.MinimumInvestment,
            Currency = fund.Currency,
            AssetManagerId = fund.AssetManagerId,
            AssetManagerName = fund.AssetManagerName,
            AssetManagerNameEn = fund.AssetManagerNameEn,
            AssetManagerCode = fund.AssetManagerCode,
            LogoUrl = fund.LogoUrl,
            LocalLogoPath = fund.LocalLogoPath,
            DescriptionAr = fund.DescriptionAr,
            DescriptionEn = fund.DescriptionEn,
            InvestmentStrategy = fund.InvestmentStrategy,
            InceptionDate = fund.InceptionDate,
            ProspectusUrl = fund.ProspectusUrl,
            LastSyncedAt = fund.LastSyncedAt,
            PriceHistory = priceHistory.Select(p => new FundPricePointDto
            {
                Date = p.Date,
                Price = p.Price,
                Change = p.Change,
                Performance = p.Performance
            }).ToList(),
            Analysis = analysis != null ? new FundAnalysisDto
            {
                PerformanceScore = analysis.PerformanceScore,
                RiskScore = analysis.RiskScore,
                CostScore = analysis.CostScore,
                StabilityScore = analysis.StabilityScore,
                DiversificationScore = analysis.DiversificationScore,
                TotalScore = analysis.TotalScore,
                Cagr = analysis.Cagr,
                Alpha = analysis.Alpha,
                Volatility = analysis.Volatility,
                Consistency = analysis.Consistency,
                MaxDrawdown = analysis.MaxDrawdown,
                SharpeRatio = analysis.SharpeRatio,
                PerformanceExplanationAr = analysis.PerformanceExplanationAr,
                PerformanceExplanationEn = analysis.PerformanceExplanationEn,
                RiskExplanationAr = analysis.RiskExplanationAr,
                RiskExplanationEn = analysis.RiskExplanationEn,
                CostExplanationAr = analysis.CostExplanationAr,
                CostExplanationEn = analysis.CostExplanationEn,
                StabilityExplanationAr = analysis.StabilityExplanationAr,
                StabilityExplanationEn = analysis.StabilityExplanationEn,
                DiversificationExplanationAr = analysis.DiversificationExplanationAr,
                DiversificationExplanationEn = analysis.DiversificationExplanationEn,
                CalculatedAt = analysis.CalculatedAt
            } : null,
            SimilarFunds = similarFunds.Select(s => new FundSimilarDto
            {
                Id = s.SimilarFundId,
                Code = s.SimilarFund?.Code ?? string.Empty,
                NameAr = s.SimilarFund?.NameAr ?? string.Empty,
                NameEn = s.SimilarFund?.NameEn ?? string.Empty,
                LocalLogoPath = s.SimilarFund?.LocalLogoPath ?? string.Empty,
                LogoUrl = s.SimilarFund?.LogoUrl ?? string.Empty
            }).ToList(),
            CategoryMemberships = categoryMemberships
        };

        FundMappingExtensions.ApplyType(result, bundle.Fund, detailOverrides, detailDefs);

        var detailCtx = await _detailQuery.LoadContextAsync(cancellationToken);
        var detailEff = await _detailQuery.ResolveEffectiveManyAsync(new[] { bundle.Fund }, detailCtx);
        FundMappingExtensions.ApplyClassification(result, bundle.Fund, detailEff);

        return result;
    }
}
