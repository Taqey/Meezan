using Meezan.Application.Features.Funds.Types.Dtos;
using Meezan.Domain.Entities;
using Meezan.Domain.Models;
using Meezan.Domain.Services;

namespace Meezan.Application.Features.Funds.Details;

public static class FundMappingExtensions
{
    public static void ApplyType(
        IFundTypeFields dto,
        Fund f,
        Dictionary<int, (int TypeId, string Slug)> overrides,
        Dictionary<string, FundTypeDefinition> defs)
    {
        var slug = FundTypeCatalog.SlugFor(f.Type);
        var isOverride = false;

        if (overrides.TryGetValue(f.Id, out var o) && defs.TryGetValue(o.Slug, out var def))
        {
            slug = o.Slug;
            dto.Type = FundTypeCatalog.EnumFor(o.Slug);
            dto.TypeNameAr = def.NameAr;
            dto.TypeNameEn = def.NameEn;
            dto.TypeId = def.Id;
            dto.TypeSlug = def.Slug;
            dto.TypeIsOverridden = true;
            isOverride = true;
        }

        if (isOverride) return;

        if (defs.TryGetValue(slug, out var d))
        {
            dto.TypeId = d.Id;
            dto.TypeSlug = d.Slug;
            if (!string.IsNullOrWhiteSpace(d.NameAr)) dto.TypeNameAr = d.NameAr;
            if (!string.IsNullOrWhiteSpace(d.NameEn)) dto.TypeNameEn = d.NameEn;
        }
        else
        {
            dto.TypeSlug = slug;
        }
    }

    public static void ApplyClassification(
        IFundTypeFields dto,
        Fund f,
        Dictionary<int, EffectiveClassification> eff)
    {
        if (eff.TryGetValue(f.Id, out var e) && e.NodeId.HasValue)
        {
            dto.ClassificationSlug = e.Slug;
            dto.ClassificationPathAr = e.DisplayPathAr;
            dto.ClassificationSource = e.Source;

            var rootSlug = e.PathSlugs.Count > 0 ? e.PathSlugs[0] : e.Slug;
            var rootNameAr = e.PathNamesAr.Count > 0 ? e.PathNamesAr[0] : string.Empty;
            var rootNameEn = e.PathNamesEn.Count > 0 ? e.PathNamesEn[0] : string.Empty;
            var leafSlug = e.PathSlugs.Count > 0 ? e.PathSlugs[^1] : e.Slug;
            var leafNameAr = e.PathNamesAr.Count > 0 ? e.PathNamesAr[^1] : string.Empty;
            var leafNameEn = e.PathNamesEn.Count > 0 ? e.PathNamesEn[^1] : string.Empty;

            dto.EffectiveClassification = new Meezan.Application.Features.Funds.Classification.Dtos.EffectiveClassificationDto
            {
                NodeId = e.NodeId,
                Slug = e.Slug,
                NameAr = leafNameAr,
                NameEn = leafNameEn,
                Path = e.DisplayPathAr,
                RootSlug = rootSlug,
                RootNameAr = rootNameAr,
                RootNameEn = rootNameEn,
                LeafSlug = leafSlug,
                LeafNameAr = leafNameAr,
                LeafNameEn = leafNameEn,
                Source = e.Source,
                PathSlugs = new List<string>(e.PathSlugs),
                PathNamesAr = new List<string>(e.PathNamesAr),
                PathNamesEn = new List<string>(e.PathNamesEn)
            };

            // Backwards compatibility for old clients reading type / typeNameAr / typeNameEn / typeSlug
            if (!string.IsNullOrWhiteSpace(rootNameAr))
            {
                dto.TypeNameAr = rootNameAr;
            }
            if (!string.IsNullOrWhiteSpace(rootNameEn))
            {
                dto.TypeNameEn = rootNameEn;
            }
            if (!string.IsNullOrWhiteSpace(rootSlug))
            {
                dto.TypeSlug = rootSlug;
                var enumVal = FundTypeCatalog.EnumFor(rootSlug);
                if (enumVal != FundType.Unknown)
                {
                    dto.Type = enumVal;
                }
            }
        }
        else
        {
            dto.ClassificationSlug = string.Empty;
            dto.ClassificationPathAr = string.Empty;
            dto.ClassificationSource = eff.TryGetValue(f.Id, out var e2) ? e2.Source : "None";
            dto.EffectiveClassification = null;
        }
    }
}
