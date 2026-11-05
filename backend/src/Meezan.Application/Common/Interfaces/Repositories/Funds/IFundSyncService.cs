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
    Task<FetchFundMissingPricesResultDto> FetchMissingPricesForFundAsync(int fundId, string triggeredBy, bool bypassCooldown = false, CancellationToken cancellationToken = default);
    Task<HistoricalDataPreviewResultDto> PreviewHistoricalDataAsync(int? fundId = null, CancellationToken cancellationToken = default);
    Task<HistoricalDataCommitResultDto> CommitHistoricalDataAsync(int? fundId = null, bool overwriteDifferent = false, CancellationToken cancellationToken = default);
    Task<CheckNewFundsResultDto> CheckNewFundsAsync(string triggeredBy, CancellationToken cancellationToken = default);
    Task<FundCompareChartDto> GetFundCompareChartAsync(int fundId, string timeframe = "1Y", CancellationToken cancellationToken = default);
}
