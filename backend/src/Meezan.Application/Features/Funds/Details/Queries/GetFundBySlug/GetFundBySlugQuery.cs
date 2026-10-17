using MediatR;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.Funds.Details.Dtos;

namespace Meezan.Application.Features.Funds.Details.Queries.GetFundBySlug;

public sealed record GetFundBySlugQuery(string Slug) : IRequest<Result<FundDetailDto>>;
