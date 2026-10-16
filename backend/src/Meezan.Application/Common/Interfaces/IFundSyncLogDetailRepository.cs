using Meezan.Domain.Entities;

namespace Meezan.Application.Common.Interfaces;

public interface IFundSyncLogDetailRepository
{
    Task AddRangeAsync(IEnumerable<FundSyncLogDetail> details, CancellationToken cancellationToken = default);
    Task<List<FundSyncLogDetail>> GetByLogIdAsync(int logId, CancellationToken cancellationToken = default);
}
