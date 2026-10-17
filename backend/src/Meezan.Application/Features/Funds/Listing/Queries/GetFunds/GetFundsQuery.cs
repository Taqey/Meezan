using Meezan.Application.Common.Results;
using MediatR;
using Meezan.Application.Features.Funds.Categories.Dtos;
using Meezan.Application.Features.Funds.Classification.Dtos;
using Meezan.Application.Features.Funds.Details;
using Meezan.Application.Features.Funds.Details.Dtos;
using Meezan.Application.Features.Funds.Listing.Dtos;
using Meezan.Application.Features.Funds.Types.Dtos;

namespace Meezan.Application.Features.Funds.Listing.Queries.GetFunds;

public sealed record GetFundsQuery : IRequest<Result<FundListResultDto>>
{
    public string? Search { get; init; }
    public string? Type { get; init; }
    public string? SubType { get; init; }
    public string? Leaf { get; init; }
    public string? Node { get; init; }
    public string? RiskLevel { get; init; }
    public long? AssetManagerId { get; init; }
    public bool? IsIslamic { get; init; }
    public bool? IsFeatured { get; init; }
    public int? CategoryId { get; init; }
    public decimal? MinManagementFee { get; init; }
    public decimal? MaxManagementFee { get; init; }
    public decimal? MinInvestment { get; init; }
    public decimal? MaxInvestment { get; init; }
    public string? SortBy { get; init; }
    public string SortDir { get; init; } = "desc";
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
