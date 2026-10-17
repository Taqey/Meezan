using MediatR;

namespace Meezan.Application.Features.Funds.Sync.Commands.BackfillFundLogos;

public sealed record BackfillFundLogosCommand : IRequest<BackfillFundLogosResultDto>;
