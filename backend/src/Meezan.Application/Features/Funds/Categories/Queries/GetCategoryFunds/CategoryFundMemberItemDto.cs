namespace Meezan.Application.Features.Funds.Categories.Queries.GetCategoryFunds;

public record CategoryFundMemberItemDto(
    int Id,
    string Code,
    string NameAr,
    string NameEn,
    string? Source,
    string Reason);
