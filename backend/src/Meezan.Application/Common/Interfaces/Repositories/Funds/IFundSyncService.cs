using Meezan.Domain.Entities;

namespace Meezan.Application.Common.Interfaces.Repositories.Funds;

public interface IFundSyncService
{
    Task<FundSyncLog> RunSyncAsync(string triggeredBy, CancellationToken cancellationToken = default);
    Task<FundSyncLog?> GetLatestRunAsync(CancellationToken cancellationToken = default);
    Task<(List<FundSyncLog> Items, int TotalCount)> GetRunHistoryAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> BackfillLogosAsync(CancellationToken cancellationToken = default);
}
