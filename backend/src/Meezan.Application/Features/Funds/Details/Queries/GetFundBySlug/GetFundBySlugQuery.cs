using Meezan.Application.Common.Results;
using MediatR;
using Meezan.Application.Features.Funds.Categories.Dtos;
using Meezan.Application.Features.Funds.Classification.Dtos;
using Meezan.Application.Features.Funds.Details;
using Meezan.Application.Features.Funds.Details.Dtos;
using Meezan.Application.Features.Funds.Listing.Dtos;
using Meezan.Application.Features.Funds.Types.Dtos;

namespace Meezan.Application.Features.Funds.Details.Queries.GetFundBySlug;

public sealed record GetFundBySlugQuery(string Slug) : IRequest<Result<FundDetailDto>>;
