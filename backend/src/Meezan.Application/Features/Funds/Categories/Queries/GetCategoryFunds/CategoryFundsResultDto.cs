using System.Collections.Generic;

namespace Meezan.Application.Features.Funds.Categories.Queries.GetCategoryFunds;

public record CategoryFundsResultDto(
    List<CategoryFundMemberItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);
