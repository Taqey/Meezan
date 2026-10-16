namespace Meezan.Application.Features.Funds.Queries.GetSyncSettings;

using MediatR;
using Meezan.Domain.Entities;

public record GetSyncSettingsQuery : IRequest<FundSyncSettings>;
