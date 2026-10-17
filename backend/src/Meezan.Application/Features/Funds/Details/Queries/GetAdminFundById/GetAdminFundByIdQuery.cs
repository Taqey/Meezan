namespace Meezan.Application.Features.Funds.Details.Queries.GetAdminFundById;

using MediatR;
using Meezan.Application.Common.Results;
using Meezan.Domain.Entities;

public record GetAdminFundByIdQuery(int Id) : IRequest<Result<Fund?>>;
