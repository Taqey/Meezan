using MediatR;
using Meezan.Application.Common.Results;

namespace Meezan.Application.Features.Funds.Types.Queries.GetFundTypeChanges;

public sealed record GetFundTypeChangesQuery(int Page = 1, int PageSize = 50) : IRequest<Result<FundTypeChangesResultDto>>;
