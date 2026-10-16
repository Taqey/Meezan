using Meezan.Domain.Entities;
using Meezan.Application.Common.Models;

namespace Meezan.Application.Common.Interfaces;

public interface IFundCategoryChangeLogRepository
{
    Task AddAsync(FundCategoryChangeLog log, CancellationToken cancellationToken = default);
    Task<(List<FundCategoryChangeLog> Items, int TotalCount)> QueryPagedAsync(FundCategoryChangeLogFilter filter, CancellationToken cancellationToken = default);
}
