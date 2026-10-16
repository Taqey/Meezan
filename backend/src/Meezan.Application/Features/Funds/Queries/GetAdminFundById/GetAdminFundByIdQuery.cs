namespace Meezan.Application.Features.Funds.Queries.GetAdminFundById;

using MediatR;
using Meezan.Domain.Entities;

public record GetAdminFundByIdQuery(int Id) : IRequest<Fund?>;
