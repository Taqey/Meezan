using MediatR;
using Meezan.Application.Common.Results;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Sync.Commands.UpdateSyncSettings;

public sealed record UpdateSyncSettingsCommand : IRequest<Result<FundSyncSettings>>
{
    public bool? IsEnabled { get; init; }
    public TimeOnly? RunTimeCairo { get; init; }
    public bool? PreventOverlap { get; init; }
    public int? RequestDelaySeconds { get; init; }
    public int? MaxRetries { get; init; }
    public int? RetryBackoffSeconds { get; init; }
    public bool? AutoIndexFundTypeEnabled { get; init; }
    public bool? AutoClassificationEnabled { get; init; }
    public bool? FullSyncScheduleEnabled { get; init; }
    public TimeOnly? FullSyncRunTimeCairo { get; init; }
}
