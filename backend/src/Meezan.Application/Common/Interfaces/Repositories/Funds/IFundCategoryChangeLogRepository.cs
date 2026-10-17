using Meezan.Domain.Entities;
using Meezan.Application.Common.Models.Funds;
using Meezan.Application.Common.Models.Stocks;

namespace Meezan.Application.Common.Interfaces.Repositories.Funds;

public interface IFundCategoryChangeLogRepository
{
    Task AddAsync(FundCategoryChangeLog log, CancellationToken cancellationToken = default);
    Task<(List<FundCategoryChangeLog> Items, int TotalCount)> QueryPagedAsync(FundCategoryChangeLogFilter filter, CancellationToken cancellationToken = default);
}
