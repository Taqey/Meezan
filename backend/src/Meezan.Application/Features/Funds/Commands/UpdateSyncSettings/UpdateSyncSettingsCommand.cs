using MediatR;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Commands.UpdateSyncSettings;

public sealed record UpdateSyncSettingsCommand : IRequest<FundSyncSettings>
{
    public bool? IsEnabled { get; init; }
    public TimeOnly? RunTimeCairo { get; init; }
    public bool? PreventOverlap { get; init; }
    public int? RequestDelaySeconds { get; init; }
    public int? MaxRetries { get; init; }
    public int? RetryBackoffSeconds { get; init; }
    public bool? AutoIndexFundTypeEnabled { get; init; }
    public bool? AutoClassificationEnabled { get; init; }
}
