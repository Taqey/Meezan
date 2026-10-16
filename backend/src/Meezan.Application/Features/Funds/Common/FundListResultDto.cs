namespace Meezan.Application.Features.Funds.Common;

public sealed class FundListResultDto
{
    public List<FundListItemDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
