namespace Meezan.Application.Features.Funds.Types.Queries.GetTypeFunds;

public sealed record TypeFundsResultDto(
    List<AdminTypeFundRowDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

