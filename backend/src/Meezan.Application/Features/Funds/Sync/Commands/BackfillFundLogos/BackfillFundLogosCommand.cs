using MediatR;
using Meezan.Application.Common.Results;

namespace Meezan.Application.Features.Funds.Sync.Commands.BackfillFundLogos;

public sealed record BackfillFundLogosCommand : IRequest<Result<BackfillFundLogosResultDto>>;
