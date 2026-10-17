using MediatR;

namespace Meezan.Application.Features.Funds.Queries.GetFundTypeChanges;

public sealed record GetFundTypeChangesQuery(int Page = 1, int PageSize = 50) : IRequest<FundTypeChangesResultDto>;
