namespace Meezan.Application.Features.Funds.Queries.GetTypeFunds;

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
    string Source,
    string? OriginalSndukSlug,
    string? OriginalSndukNameAr,
    int? OverrideTypeId,
    List<FundCategoryChipDto> Categories);

public sealed record TypeFundsResultDto(
    List<AdminTypeFundRowDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);
