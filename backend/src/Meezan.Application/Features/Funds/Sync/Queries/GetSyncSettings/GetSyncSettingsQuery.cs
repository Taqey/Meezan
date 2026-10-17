namespace Meezan.Application.Features.Funds.Sync.Queries.GetSyncSettings;

using MediatR;
using Meezan.Domain.Entities;

public record GetSyncSettingsQuery : IRequest<FundSyncSettings>;
