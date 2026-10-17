namespace Meezan.Application.Features.Funds.Categories.Queries.GetCategoryFunds;

using MediatR;

public record GetCategoryFundsQuery(
    int CategoryId,
    string? Search,
    string? Source,
    int Page,
    int PageSize) : IRequest<CategoryFundsResultDto?>;

public record CategoryFundMemberItemDto(
    int Id,
    string Code,
    string NameAr,
    string NameEn,
    string? Source,
    string Reason);

public record CategoryFundsResultDto(
    List<CategoryFundMemberItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);
