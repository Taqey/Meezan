using System.Collections.Generic;

namespace Meezan.Application.Features.Funds.Types.Queries.GetTypeFunds;

public sealed record AdminTypeFundRowDto(
    int Id,
    string Code,
    string NameAr,
    string NameEn,
    string AssetManagerName,
    int? EffectiveTypeId,
    string EffectiveTypeSlug,
    string EffectiveTypeNameAr,
    string Source,
    string? OriginalSndukSlug,
    string? OriginalSndukNameAr,
    int? OverrideTypeId,
    List<FundCategoryChipDto> Categories);
