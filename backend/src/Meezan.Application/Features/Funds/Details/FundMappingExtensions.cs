using Meezan.Application.Features.Funds.Categories.Dtos;
using Meezan.Application.Features.Funds.Classification.Dtos;
using Meezan.Application.Features.Funds.Details;
using Meezan.Application.Features.Funds.Details.Dtos;
using Meezan.Application.Features.Funds.Listing.Dtos;
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
        }
        else
        {
            dto.ClassificationSlug = string.Empty;
            dto.ClassificationPathAr = string.Empty;
            dto.ClassificationSource = eff.TryGetValue(f.Id, out var e2) ? e2.Source : "None";
        }
    }
}
