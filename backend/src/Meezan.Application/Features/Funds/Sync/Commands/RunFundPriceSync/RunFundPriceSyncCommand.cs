using MediatR;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.Funds.Sync.Dtos;

namespace Meezan.Application.Features.Funds.Sync.Commands.RunFundPriceSync;

public sealed record RunFundPriceSyncCommand(string TriggeredBy = "Manual") : IRequest<Result<FundPriceSyncResultDto>>;
