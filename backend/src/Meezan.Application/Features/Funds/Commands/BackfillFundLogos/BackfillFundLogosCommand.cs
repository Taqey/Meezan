using MediatR;

namespace Meezan.Application.Features.Funds.Commands.BackfillFundLogos;

public sealed record BackfillFundLogosCommand : IRequest<BackfillFundLogosResultDto>;
