using MediatR;

namespace Meezan.Application.Features.Funds.Types.Queries.GetTypeFunds;

public sealed record GetTypeFundsQuery(
    int Id,
    string? Search,
    int Page = 1,
    int PageSize = 50) : IRequest<TypeFundsResultDto?>;
