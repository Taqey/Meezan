using Meezan.Domain.Entities;

namespace Meezan.Application.Common.Interfaces.Repositories.Funds;

public interface IFundSyncSettingsRepository
{
    Task<FundSyncSettings?> GetAsync(CancellationToken cancellationToken = default);
    void Update(FundSyncSettings settings);
}
