namespace Meezan.Application.Features.Funds.Details.Queries.GetAdminFundById;

using MediatR;
using Meezan.Domain.Entities;

public record GetAdminFundByIdQuery(int Id) : IRequest<Fund?>;
