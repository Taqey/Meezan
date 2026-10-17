using Meezan.Domain.Entities;

namespace Meezan.Application.Common.Interfaces;

public interface IFundTypeChangeLogRepository
{
    Task AddAsync(FundTypeChangeLog log, CancellationToken cancellationToken = default);
    Task<(List<FundTypeChangeLog> Items, int TotalCount)> QueryPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
}
