using Meezan.Domain.Entities;
using Meezan.Application.Features.Funds.Sync.Dtos;

namespace Meezan.Application.Common.Interfaces.Repositories.Funds;

public interface IFundSyncService
{
    Task<FundSyncLog> RunSyncAsync(string triggeredBy, CancellationToken cancellationToken = default);
    Task<FundPriceSyncResultDto> RunPriceSyncAsync(string triggeredBy, CancellationToken cancellationToken = default);
    Task<FundSyncLog?> GetLatestRunAsync(CancellationToken cancellationToken = default);
    Task<(List<FundSyncLog> Items, int TotalCount)> GetRunHistoryAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> BackfillLogosAsync(CancellationToken cancellationToken = default);
    Task<FundPriceHistoryBackfillResultDto> BackfillPriceHistoryAsync(bool dryRun = true, int? specificFundId = null, CancellationToken cancellationToken = default);
}
