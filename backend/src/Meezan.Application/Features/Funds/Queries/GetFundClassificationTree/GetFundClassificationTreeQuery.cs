using MediatR;
using Meezan.Application.Features.Funds.Common;

namespace Meezan.Application.Features.Funds.Queries.GetFundClassificationTree;

public sealed record GetFundClassificationTreeQuery : IRequest<List<FundClassificationTreeDto>>;
