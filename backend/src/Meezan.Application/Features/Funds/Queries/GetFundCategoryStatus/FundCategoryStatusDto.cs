namespace Meezan.Application.Features.Funds.Queries.GetFundCategoryStatus;

public sealed record FundCategoryInfoDto(
    int Id,
    string NameAr,
    string NameEn,
    string Slug,
    bool IsAutoDerived);

public sealed record FundCategoryStatusDto(
    FundCategoryInfoDto Category,
    bool IsIncluded,
    string Source,
    int? OverrideId);
