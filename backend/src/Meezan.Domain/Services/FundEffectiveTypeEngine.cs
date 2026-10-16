using Meezan.Domain.Entities;
using Meezan.Domain.Models;

namespace Meezan.Domain.Services;

/// <summary>
/// Single shared engine for calculating the effective fund type across the system:
/// Precedence:
/// 1. Manual operator override in FundTypeOverrides (يدوي)
/// 2. If AutoIndexFundTypeEnabled is true, and fund belongs to an IsIndexTracking category
///    under final membership (including auto matches unless excluded, plus manual includes) (تلقائي - يتبع مؤشر) -> index
/// 3. Original SNDUK fund type (SNDUK)
/// </summary>
public static class FundEffectiveTypeEngine
{
    public const string SourceSnduk = "SNDUK";
    public const string SourceManual = "يدوي";
    public const string SourceIndexCategory = "تلقائي - يتبع مؤشر";

    public static EffectiveFundTypeResult Calculate(
        Fund fund,
        IReadOnlyDictionary<int, FundTypeDefinition> definitionsById,
        IReadOnlyDictionary<string, FundTypeDefinition> definitionsBySlug,
        IReadOnlyDictionary<int, int> manualTypeOverrideByFundId,
        bool autoIndexEnabled,
        ISet<int> indexTrackingFundIds)
    {
        var originalSlug = FundTypeCatalog.SlugFor(fund.Type);
        var originalDef = definitionsBySlug.TryGetValue(originalSlug, out var od) ? od : null;
        var originalNameAr = originalDef?.NameAr ?? (!string.IsNullOrWhiteSpace(fund.TypeNameAr) ? fund.TypeNameAr : originalSlug);

        // 1. Manual operator override
        if (manualTypeOverrideByFundId.TryGetValue(fund.Id, out var overrideDefId) &&
            definitionsById.TryGetValue(overrideDefId, out var overrideDef))
        {
            return new EffectiveFundTypeResult(
                overrideDef.Id,
                overrideDef.Slug,
                overrideDef.NameAr,
                overrideDef.NameEn,
                SourceManual,
                originalSlug,
                originalNameAr
            );
        }

        // 2. Index-tracking category derivation
        if (autoIndexEnabled && indexTrackingFundIds.Contains(fund.Id))
        {
            var indexDef = definitionsBySlug.TryGetValue("index", out var idxD) ? idxD : null;
            return new EffectiveFundTypeResult(
                indexDef?.Id,
                "index",
                indexDef?.NameAr ?? "صندوق مؤشر",
                indexDef?.NameEn ?? "Index Fund",
                SourceIndexCategory,
                originalSlug,
                originalNameAr
            );
        }

        // 3. Original SNDUK type
        return new EffectiveFundTypeResult(
            originalDef?.Id,
            originalSlug,
            originalNameAr,
            originalDef?.NameEn ?? (!string.IsNullOrWhiteSpace(fund.TypeNameEn) ? fund.TypeNameEn : originalSlug),
            SourceSnduk,
            originalSlug,
            originalNameAr
        );
    }
}
