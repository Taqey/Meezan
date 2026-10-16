namespace Meezan.Application.Features.Funds.Queries.GetSyncSettings;

using MediatR;
using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Entities;

public class GetSyncSettingsQueryHandler : IRequestHandler<GetSyncSettingsQuery, FundSyncSettings>
{
    private readonly IFundSyncSettingsRepository _settingsRepo;

    public GetSyncSettingsQueryHandler(IFundSyncSettingsRepository settingsRepo)
    {
        _settingsRepo = settingsRepo;
    }

    public async Task<FundSyncSettings> Handle(GetSyncSettingsQuery request, CancellationToken cancellationToken)
    {
        return await _settingsRepo.GetAsync(cancellationToken) ?? new FundSyncSettings { Id = 1 };
    }
}
