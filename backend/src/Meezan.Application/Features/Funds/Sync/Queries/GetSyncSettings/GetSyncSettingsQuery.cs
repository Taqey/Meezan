namespace Meezan.Application.Features.Funds.Sync.Queries.GetSyncSettings;

using MediatR;
using Meezan.Application.Common.Results;
using Meezan.Domain.Entities;

public record GetSyncSettingsQuery : IRequest<Result<FundSyncSettings>>;
