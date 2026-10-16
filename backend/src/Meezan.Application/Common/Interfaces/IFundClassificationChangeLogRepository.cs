using Meezan.Domain.Entities;

namespace Meezan.Application.Common.Interfaces;

public interface IFundClassificationChangeLogRepository
{
    Task AddAsync(FundClassificationChangeLog log, CancellationToken cancellationToken = default);
    Task<(List<FundClassificationChangeLog> Items, int TotalCount)> QueryPagedAsync(int? fundId, int? nodeId, ClassificationChangeAction? action, int page, int pageSize, CancellationToken cancellationToken = default);
}
