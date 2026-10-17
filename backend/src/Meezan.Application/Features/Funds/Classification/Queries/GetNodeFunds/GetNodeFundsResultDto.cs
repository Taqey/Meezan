using Meezan.Application.Features.Funds.Classification.Dtos;

namespace Meezan.Application.Features.Funds.Classification.Queries.GetNodeFunds;

public sealed class GetNodeFundsResultDto
{
    public AdminNodeBriefDto Node { get; set; } = new();
    public List<AdminNodeBriefDto> Path { get; set; } = new();
    public List<AdminNodeFundRowDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
