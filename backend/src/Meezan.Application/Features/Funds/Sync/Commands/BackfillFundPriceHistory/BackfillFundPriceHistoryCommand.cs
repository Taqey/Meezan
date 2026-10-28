using MediatR;
using Meezan.Application.Common.Interfaces.Caching;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.Funds.Sync.Dtos;

namespace Meezan.Application.Features.Funds.Sync.Commands.BackfillFundPriceHistory;

public sealed record BackfillFundPriceHistoryCommand(bool DryRun = true, int? SpecificFundId = null)
    : IRequest<Result<FundPriceHistoryBackfillResultDto>>, IInvalidatesFundPrices;

public sealed record FetchFundMissingPricesCommand(int FundId, string TriggeredBy = "Admin", bool BypassCooldown = false)
    : IRequest<Result<FetchFundMissingPricesResultDto>>;


