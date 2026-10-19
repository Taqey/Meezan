using MediatR;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.Funds.Listing.Dtos;

namespace Meezan.Application.Features.Funds.Listing.Queries.GetFundAssetManagers;

public sealed record GetFundAssetManagersQuery : IRequest<Result<List<FundAssetManagerOptionDto>>>;
