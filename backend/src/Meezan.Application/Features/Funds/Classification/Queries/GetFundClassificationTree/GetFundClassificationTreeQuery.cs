using MediatR;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.Funds.Classification.Dtos;

namespace Meezan.Application.Features.Funds.Classification.Queries.GetFundClassificationTree;

public sealed record GetFundClassificationTreeQuery : IRequest<Result<List<FundClassificationTreeDto>>>;
