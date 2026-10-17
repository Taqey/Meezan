namespace Meezan.Application.Features.Funds.Sync.Queries.GetSyncSettings;

using MediatR;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Results;
using Meezan.Domain.Entities;

public class GetSyncSettingsQueryHandler : IRequestHandler<GetSyncSettingsQuery, Result<FundSyncSettings>>
{
    private readonly IFundSyncSettingsRepository _settingsRepo;

    public GetSyncSettingsQueryHandler(IFundSyncSettingsRepository settingsRepo)
    {
        _settingsRepo = settingsRepo;
    }

    public async Task<Result<FundSyncSettings>> Handle(GetSyncSettingsQuery request, CancellationToken cancellationToken)
    {
        return await _settingsRepo.GetAsync(cancellationToken) ?? new FundSyncSettings { Id = 1 };
    }
}
