namespace Meezan.Application.Features.Funds.Categories.Queries.GetFundCategoryStatus;

public sealed record FundCategoryStatusDto(
    FundCategoryInfoDto Category,
    bool IsIncluded,
    string Source,
    int? OverrideId);
