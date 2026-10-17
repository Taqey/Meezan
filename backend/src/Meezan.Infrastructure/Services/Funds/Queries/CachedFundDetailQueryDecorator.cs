using Meezan.Application.Common.Interfaces.Caching;
using Meezan.Application.Common.Interfaces.Queries.Funds;
using Meezan.Application.Common.Models.Funds;
using Meezan.Domain.Entities;
using Meezan.Domain.Models;

namespace Meezan.Infrastructure.Services.Funds.Queries;

public class CachedFundDetailQueryDecorator : IFundDetailQuery
{
    private readonly IFundDetailQuery _inner;
    private readonly IFundCacheService _cache;

    public CachedFundDetailQueryDecorator(IFundDetailQuery inner, IFundCacheService cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public async Task<FundDetailBundle?> LoadAsync(int fundId, CancellationToken ct = default)
    {
        var staticFunds = await _cache.GetStaticFundsAsync(ct);
        if (!staticFunds.TryGetValue(fundId, out var s))
        {
            // Cold path fallback to inner SQL query
            return await _inner.LoadAsync(fundId, ct);
        }

        var prices = await _cache.GetPricesSnapshotAsync(ct);
        prices.TryGetValue(fundId, out var p);

        var priceHistory = await _cache.GetPriceHistoryAsync(fundId, ct);

        // Reconstruct Fund entity
        var fund = new Fund
        {
            Id = s.Id,
            SndukId = s.SndukId,
            PublicId = s.PublicId,
            Slug = s.Slug,
            NameAr = s.NameAr,
            NameEn = s.NameEn,
            Code = s.Code,
            Type = s.Type,
            TypeNameAr = s.TypeNameAr,
            TypeNameEn = s.TypeNameEn,
            RiskLevel = s.RiskLevel,
            IsIslamic = s.IsIslamic,
            IsFeatured = s.IsFeatured,
            IsActive = s.IsActive,
            CurrentPrice = p?.CurrentPrice,
            DailyChange = p?.DailyChange,
            Return1M = p?.Return1M,
            Return3M = p?.Return3M,
            Return6M = p?.Return6M,
            Return1Y = p?.Return1Y,
            Return3Y = p?.Return3Y,
            TotalScore = s.TotalScore,
            Aum = s.Aum,
            ManagementFee = s.ManagementFee,
            MinimumInvestment = s.MinimumInvestment,
            Currency = s.Currency,
            AssetManagerId = s.AssetManagerId,
            AssetManagerName = s.AssetManagerName,
            AssetManagerNameEn = s.AssetManagerNameEn,
            AssetManagerCode = s.AssetManagerCode,
            LogoUrl = s.LogoUrl,
            LocalLogoPath = s.LocalLogoPath,
            DescriptionAr = s.DescriptionAr,
            DescriptionEn = s.DescriptionEn,
            InvestmentStrategy = s.InvestmentStrategy,
            InceptionDate = s.InceptionDate,
            ProspectusUrl = s.ProspectusUrl,
            LastSyncedAt = s.LastSyncedAt,
            PricesUpdatedAt = p?.PricesUpdatedAt
        };

        // Price history entities
        var historyEntities = priceHistory.Select(h => new FundPriceHistory
        {
            FundId = fundId,
            Date = h.Date,
            Price = h.Price,
            Change = h.Change,
            Performance = h.Performance
        }).ToList();

        // Analysis entity
        FundAnalysis? analysisEntity = null;
        if (s.Analysis != null)
        {
            analysisEntity = new FundAnalysis
            {
                FundId = fundId,
                PerformanceScore = s.Analysis.PerformanceScore,
                RiskScore = s.Analysis.RiskScore,
                CostScore = s.Analysis.CostScore,
                StabilityScore = s.Analysis.StabilityScore,
                DiversificationScore = s.Analysis.DiversificationScore,
                TotalScore = s.Analysis.TotalScore,
                Cagr = s.Analysis.Cagr,
                Alpha = s.Analysis.Alpha,
                Volatility = s.Analysis.Volatility,
                Consistency = s.Analysis.Consistency,
                MaxDrawdown = s.Analysis.MaxDrawdown,
                SharpeRatio = s.Analysis.SharpeRatio,
                WeightsJson = s.Analysis.WeightsJson,
                PerformanceExplanationAr = s.Analysis.PerformanceExplanationAr,
                PerformanceExplanationEn = s.Analysis.PerformanceExplanationEn,
                RiskExplanationAr = s.Analysis.RiskExplanationAr,
                RiskExplanationEn = s.Analysis.RiskExplanationEn,
                CostExplanationAr = s.Analysis.CostExplanationAr,
                CostExplanationEn = s.Analysis.CostExplanationEn,
                StabilityExplanationAr = s.Analysis.StabilityExplanationAr,
                StabilityExplanationEn = s.Analysis.StabilityExplanationEn,
                DiversificationExplanationAr = s.Analysis.DiversificationExplanationAr,
                DiversificationExplanationEn = s.Analysis.DiversificationExplanationEn,
                CalculatedAt = s.Analysis.CalculatedAt
            };
        }

        // Similars
        var similarEntities = new List<FundSimilar>();
        foreach (var simId in s.SimilarFundIds)
        {
            if (staticFunds.TryGetValue(simId, out var simStatic))
            {
                similarEntities.Add(new FundSimilar
                {
                    FundId = fundId,
                    SimilarFundId = simId,
                    SimilarFund = new Fund
                    {
                        Id = simStatic.Id,
                        Code = simStatic.Code,
                        NameAr = simStatic.NameAr,
                        NameEn = simStatic.NameEn,
                        Slug = simStatic.Slug,
                        LogoUrl = simStatic.LogoUrl,
                        LocalLogoPath = simStatic.LocalLogoPath,
                        CurrentPrice = prices.TryGetValue(simId, out var simP) ? simP.CurrentPrice : null,
                        Return1Y = prices.TryGetValue(simId, out var simP2) ? simP2.Return1Y : null
                    }
                });
            }
        }

        // Classification dataset for overrides and type defs
        var classDataset = await _cache.GetClassificationDatasetAsync(ct);
        var typeDefs = classDataset.TypeDefsBySlug.Values.Select(d => new FundTypeDefinition
        {
            Id = d.Id,
            Slug = d.Slug,
            NameAr = d.NameAr,
            NameEn = d.NameEn,
            DisplayOrder = d.DisplayOrder,
            IsVisible = d.IsVisible,
            IsDeleted = d.IsDeleted
        }).ToList();

        var typeOverrides = classDataset.TypeOverrides
            .Where(kv => kv.Key == fundId)
            .Select(kv => new FundTypeOverride
            {
                FundId = fundId,
                FundTypeDefinitionId = kv.Value.TypeId
            }).ToList();

        return new FundDetailBundle
        {
            Fund = fund,
            PriceHistory = historyEntities,
            Analysis = analysisEntity,
            SimilarFunds = similarEntities,
            Categories = new List<FundCategory>(),
            Overrides = s.CategoryIds.Select(cid => new FundCategoryOverride
            {
                FundId = fundId,
                CategoryId = cid,
                Action = OverrideAction.Include
            }).ToList(),
            TypeDefinitions = typeDefs,
            TypeOverrides = typeOverrides
        };
    }

    public Task<ClassificationContext> LoadContextAsync(CancellationToken ct = default)
    {
        return _inner.LoadContextAsync(ct);
    }

    public Task<Dictionary<int, EffectiveClassification>> ResolveEffectiveManyAsync(IEnumerable<Fund> funds, ClassificationContext ctx)
    {
        return _inner.ResolveEffectiveManyAsync(funds, ctx);
    }
}
