using Meezan.Domain.Entities;

namespace Meezan.Application.Common.Interfaces;

public interface IFundSyncLogRepository
{
    Task<FundSyncLog?> GetLatestAsync(CancellationToken cancellationToken = default);
    Task<(List<FundSyncLog> Items, int TotalCount)> QueryPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task AddAsync(FundSyncLog log, CancellationToken cancellationToken = default);
    void Update(FundSyncLog log);
    FundSyncLog? LastAdded { get; }
}
