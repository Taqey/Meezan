using MediatR;
using Meezan.Application.Common.Results;

namespace Meezan.Application.Features.Funds.Classification.Queries.GetNodeFunds;

public sealed record GetNodeFundsQuery(
    int Id,
    string? Search = null,
    string? Source = null,
    int Page = 1,
    int PageSize = 25) : IRequest<Result<GetNodeFundsResultDto>>;
