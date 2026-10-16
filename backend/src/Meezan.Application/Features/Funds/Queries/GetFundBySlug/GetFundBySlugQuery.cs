using MediatR;
using Meezan.Application.Features.Funds.Common;

namespace Meezan.Application.Features.Funds.Queries.GetFundBySlug;

public sealed record GetFundBySlugQuery(string Slug) : IRequest<FundDetailDto?>;
