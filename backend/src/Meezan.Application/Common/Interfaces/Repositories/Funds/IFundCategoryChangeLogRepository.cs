using Meezan.Application.Common.Models.Funds;
using Meezan.Domain.Entities;

namespace Meezan.Application.Common.Interfaces.Repositories.Funds;

public interface IFundCategoryChangeLogRepository
{
    Task AddAsync(FundCategoryChangeLog log, CancellationToken cancellationToken = default);
    Task<(List<FundCategoryChangeLog> Items, int TotalCount)> QueryPagedAsync(FundCategoryChangeLogFilter filter, CancellationToken cancellationToken = default);
}
